const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const { spawn } = require('child_process');
const Store = require('electron-store');
const log = require('electron-log');
const { autoUpdater } = require('electron-updater');

// Initialize store
const store = new Store();

// Configure logging
log.transports.file.level = 'info';
log.info('App starting...');

// Backend process management
let backendProcess = null;
const BACKEND_PORT = 5000;

function startBackend() {
    const isDev = process.argv.includes('--dev');
    let backendPath;
    
    if (isDev) {
        // Development: use dotnet run
        backendPath = 'dotnet';
        const args = ['run', '--urls', `http://localhost:${BACKEND_PORT}`];
        log.info('Starting backend in development mode...');
    } else {
        // Production: use the bundled executable from extraResources
        const backendExe = path.join(process.resourcesPath, 'bin', 'Release', 'net8.0', 'win-x64', 'publish', 'ExcelReplacement.exe');
        backendPath = backendExe;
        const args = ['--urls', `http://localhost:${BACKEND_PORT}`];
        log.info('Starting backend from bundled executable...');
    }
    
    try {
        backendProcess = spawn(backendPath, isDev ? ['run', '--urls', `http://localhost:${BACKEND_PORT}`] : ['--urls', `http://localhost:${BACKEND_PORT}`], {
            stdio: ['ignore', 'pipe', 'pipe'],
            shell: isDev
        });
        
        backendProcess.stdout.on('data', (data) => {
            log.info(`Backend: ${data}`);
        });
        
        backendProcess.stderr.on('data', (data) => {
            log.error(`Backend error: ${data}`);
        });
        
        backendProcess.on('close', (code) => {
            log.info(`Backend process exited with code ${code}`);
            backendProcess = null;
        });
        
        backendProcess.on('error', (err) => {
            log.error(`Failed to start backend: ${err.message}`);
            backendProcess = null;
        });
        
        log.info('Backend started successfully');
    } catch (error) {
        log.error(`Error starting backend: ${error.message}`);
    }
}

function stopBackend() {
    if (backendProcess) {
        log.info('Stopping backend...');
        backendProcess.kill();
        backendProcess = null;
    }
}

// Handle auto-updates
autoUpdater.logger = log;
autoUpdater.autoDownload = false;

function createWindow() {
    const mainWindow = new BrowserWindow({
        width: 800,
        height: 600,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        }
    });

    mainWindow.loadFile('index.html');

    // Check for updates
    autoUpdater.checkForUpdates();

    // Open DevTools in development
    if (process.argv.includes('--dev')) {
        mainWindow.webContents.openDevTools();
    }
}

// Auto-updater events
autoUpdater.on('update-available', () => {
    log.info('Update available');
    dialog.showMessageBox({
        type: 'info',
        title: 'Update Available',
        message: 'A new version is available. Would you like to download it now?',
        buttons: ['Yes', 'No']
    }).then(result => {
        if (result.response === 0) {
            autoUpdater.downloadUpdate();
        }
    });
});

autoUpdater.on('update-downloaded', () => {
    dialog.showMessageBox({
        type: 'info',
        title: 'Update Ready',
        message: 'A new version has been downloaded. Restart the application to apply the updates.',
        buttons: ['Restart', 'Later']
    }).then(result => {
        if (result.response === 0) {
            autoUpdater.quitAndInstall();
        }
    });
});

// IPC handlers
ipcMain.handle('select-csv-file', async () => {
    const result = await dialog.showOpenDialog({
        properties: ['openFile'],
        filters: [
            { name: 'CSV Files', extensions: ['csv'] },
            { name: 'All Files', extensions: ['*'] }
        ]
    });
    return result;
});

ipcMain.handle('select-template-file', async (event, type) => {
    const filters = type === 'excel' 
        ? [
            { name: 'Excel Files', extensions: ['xlsx', 'xls'] },
            { name: 'All Files', extensions: ['*'] }
        ]
        : [
            { name: 'Word Files', extensions: ['docx', 'doc'] },
            { name: 'All Files', extensions: ['*'] }
        ];
    
    const result = await dialog.showOpenDialog({
        properties: ['openFile'],
        filters
    });
    return result;
});

ipcMain.handle('select-output-directory', async () => {
    const result = await dialog.showOpenDialog({
        properties: ['openDirectory']
    });
    return result;
});

ipcMain.handle('get-settings', () => {
    return store.get('settings') || {
        fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]',
        lastUsedDirectory: ''
    };
});

ipcMain.handle('save-settings', (event, settings) => {
    store.set('settings', settings);
    return true;
});

app.whenReady().then(() => {
    // Start the backend first
    startBackend();
    
    // Wait a moment for backend to start, then create window
    setTimeout(() => {
        createWindow();
    }, 2000);
});

app.on('window-all-closed', () => {
    stopBackend();
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
});

app.on('before-quit', () => {
    stopBackend();
}); 
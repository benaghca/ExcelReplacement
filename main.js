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
    // Always use development mode for now (dotnet run)
    const backendPath = 'dotnet';
    const args = ['run', '--urls', `http://localhost:${BACKEND_PORT}`];
    log.info('Starting backend in development mode...');
    
    try {
        backendProcess = spawn(backendPath, args, {
            stdio: ['ignore', 'pipe', 'pipe'],
            shell: true
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
        width: 1000,
        height: 700,
        minWidth: 800,
        minHeight: 600,
        show: false, // Don't show until ready
        titleBarStyle: 'default',
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false,
            webSecurity: false
        },
        icon: path.join(__dirname, 'assets', 'icon.ico')
    });

    // Show window when ready to prevent flickering
    mainWindow.once('ready-to-show', () => {
        mainWindow.show();
        log.info('Main window shown');
    });

    // Handle window closed
    mainWindow.on('closed', () => {
        stopBackend();
    });

    // Load the HTML file
    mainWindow.loadFile('index.html').then(() => {
        log.info('HTML loaded successfully');
    }).catch((error) => {
        log.error(`Error loading HTML: ${error.message}`);
    });

    // Check for updates
    autoUpdater.checkForUpdates();

    // Open DevTools in development
    if (process.argv.includes('--dev')) {
        mainWindow.webContents.openDevTools();
    }

    return mainWindow;
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
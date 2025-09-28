const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('path');
const Store = require('electron-store');
const log = require('electron-log');
const { autoUpdater } = require('electron-updater');

// Initialize store
const store = new Store();

// Configure logging
log.transports.file.level = 'info';
log.info('App starting...');

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

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
    if (process.platform !== 'darwin') {
        app.quit();
    }
});

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
}); 
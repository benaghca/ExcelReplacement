import { app, BrowserWindow, ipcMain, dialog, OpenDialogReturnValue, MessageBoxReturnValue } from 'electron';
import path from 'path';
import Store from 'electron-store';
import log from 'electron-log';
import { autoUpdater } from 'electron-updater';

// Set up IPC handlers for file dialogs
function setupIpcHandlers(): void {
    ipcMain.handle('select-csv-file', async () => {
        const result = await dialog.showOpenDialog({
            properties: ['openFile'],
            filters: [
                { name: 'CSV Files', extensions: ['csv'] },
                { name: 'All Files', extensions: ['*'] }
            ]
        });
        return result.canceled ? null : result.filePaths[0];
    });

    ipcMain.handle('select-template-file', async (event, type: string) => {
        const filters = type === 'excel' 
            ? [{ name: 'Excel Files', extensions: ['xlsx', 'xls'] }]
            : [{ name: 'Word Files', extensions: ['docx', 'doc'] }];
            
        const result = await dialog.showOpenDialog({
            properties: ['openFile'],
            filters: [
                ...filters,
                { name: 'All Files', extensions: ['*'] }
            ]
        });
        return result.canceled ? null : result.filePaths[0];
    });

    ipcMain.handle('select-output-directory', async () => {
        const result = await dialog.showOpenDialog({
            properties: ['openDirectory']
        });
        return result.canceled ? null : result.filePaths[0];
    });
}

// Initialize store
const store = new Store();

// Configure logging
log.transports.file.level = 'info';
log.info('App starting...');

// Handle auto-updates
autoUpdater.logger = log;
autoUpdater.autoDownload = false;

function createWindow(): void {
    const mainWindow = new BrowserWindow({
        width: 800,
        height: 600,
        webPreferences: {
            nodeIntegration: true,
            contextIsolation: false
        }
    });

    // Set up IPC handlers
    setupIpcHandlers();

    mainWindow.loadFile('index.html');

    // Check for updates
    autoUpdater.checkForUpdates();

    // Open DevTools in development
    const nodeProcess = (process as unknown) as NodeJS.Process;
    if (nodeProcess.argv.includes('--dev')) {
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
    }).then((result: MessageBoxReturnValue) => {
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
    }).then((result: MessageBoxReturnValue) => {
        if (result.response === 0) {
            autoUpdater.quitAndInstall();
        }
    });
});

// IPC handlers
ipcMain.handle('select-csv-file', async (): Promise<string | null> => {
    const result: OpenDialogReturnValue = await dialog.showOpenDialog({
        properties: ['openFile'],
        filters: [{ name: 'CSV Files', extensions: ['csv'] }]
    });
    return result.canceled ? null : result.filePaths[0];
});

ipcMain.handle('select-template-file', async (_event, type: string): Promise<string | null> => {
    const filters = type === 'excel' 
        ? [{ name: 'Excel Files', extensions: ['xlsx', 'xls'] }]
        : [{ name: 'Word Files', extensions: ['docx', 'doc'] }];
    
    const result: OpenDialogReturnValue = await dialog.showOpenDialog({
        properties: ['openFile'],
        filters
    });
    return result.canceled ? null : result.filePaths[0];
});

ipcMain.handle('select-output-directory', async (): Promise<string | null> => {
    const result: OpenDialogReturnValue = await dialog.showOpenDialog({
        properties: ['openDirectory']
    });
    return result.canceled ? null : result.filePaths[0];
});

ipcMain.handle('get-settings', () => {
    return store.get('settings') || {
        fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]',
        lastUsedDirectory: ''
    };
});

ipcMain.handle('save-settings', (_event, settings: any) => {
    store.set('settings', settings);
    return true;
});

app.whenReady().then(createWindow);

app.on('window-all-closed', () => {
    const nodeProcess = (process as unknown) as NodeJS.Process;
    if (nodeProcess.platform !== 'darwin') {
        app.quit();
    }
});

app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
        createWindow();
    }
}); 
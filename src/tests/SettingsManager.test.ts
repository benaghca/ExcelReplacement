import '@jest/globals';
import { SettingsManager } from '../services/SettingsManager';
import { ipcRenderer } from 'electron';
import { resetMocks } from './testUtils';

jest.mock('electron', () => ({
    ipcRenderer: {
        invoke: jest.fn()
    }
}));

describe('SettingsManager', () => {
    let settingsManager: SettingsManager;
    const mockSettings = {
        fileNamePattern: '[Test] [Pattern]',
        lastUsedDirectory: '/test/dir'
    };

    beforeEach(() => {
        resetMocks();
        settingsManager = new SettingsManager();
        (ipcRenderer.invoke as jest.Mock).mockClear();
    });

    test('should load settings from IPC', async () => {
        (ipcRenderer.invoke as jest.Mock).mockResolvedValueOnce(mockSettings);
        const settings = await settingsManager.loadSettings();
        expect(settings).toEqual(mockSettings);
        expect(ipcRenderer.invoke).toHaveBeenCalledWith('get-settings');
    });

    test('should return default settings if IPC fails', async () => {
        (ipcRenderer.invoke as jest.Mock).mockRejectedValueOnce(new Error('IPC error'));
        const settings = await settingsManager.loadSettings();
        expect(settings).toEqual({
            fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]',
            lastUsedDirectory: ''
        });
    });

    test('should save settings via IPC', async () => {
        await settingsManager.saveSettings(mockSettings);
        expect(ipcRenderer.invoke).toHaveBeenCalledWith('save-settings', mockSettings);
    });

    test('should merge partial settings with existing settings', async () => {
        (ipcRenderer.invoke as jest.Mock).mockResolvedValueOnce(mockSettings);
        await settingsManager.loadSettings();

        const newSettings = { fileNamePattern: '[New] [Pattern]' };
        await settingsManager.saveSettings(newSettings);

        expect(ipcRenderer.invoke).toHaveBeenCalledWith('save-settings', {
            fileNamePattern: '[New] [Pattern]',
            lastUsedDirectory: '/test/dir'
        });
    });

    test('should get file name pattern', async () => {
        (ipcRenderer.invoke as jest.Mock).mockResolvedValueOnce(mockSettings);
        await settingsManager.loadSettings();
        expect(settingsManager.getFileNamePattern()).toBe('[Test] [Pattern]');
    });

    test('should get last used directory', async () => {
        (ipcRenderer.invoke as jest.Mock).mockResolvedValueOnce(mockSettings);
        await settingsManager.loadSettings();
        expect(settingsManager.getLastUsedDirectory()).toBe('/test/dir');
    });
}); 
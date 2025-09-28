import { Settings } from '../types/api';
import { ipcRenderer } from 'electron';

export class SettingsManager {
    private settings: Settings | null = null;
    private defaultSettings: Settings = {
        fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]',
        lastUsedDirectory: ''
    };

    public async loadSettings(): Promise<Settings> {
        try {
            this.settings = await ipcRenderer.invoke('get-settings');
            return this.settings || this.defaultSettings;
        } catch (error) {
            console.error('Failed to load settings:', error);
            return this.defaultSettings;
        }
    }

    public async saveSettings(settings: Partial<Settings>): Promise<void> {
        try {
            const newSettings: Settings = {
                fileNamePattern: settings.fileNamePattern ?? this.settings?.fileNamePattern ?? this.defaultSettings.fileNamePattern,
                lastUsedDirectory: settings.lastUsedDirectory ?? this.settings?.lastUsedDirectory ?? this.defaultSettings.lastUsedDirectory
            };
            this.settings = newSettings;
            await ipcRenderer.invoke('save-settings', this.settings);
        } catch (error) {
            console.error('Failed to save settings:', error);
            throw error;
        }
    }

    public getFileNamePattern(): string {
        return this.settings?.fileNamePattern || this.defaultSettings.fileNamePattern;
    }

    public getLastUsedDirectory(): string {
        return this.settings?.lastUsedDirectory || this.defaultSettings.lastUsedDirectory;
    }
} 
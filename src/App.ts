import { FileInput } from './components/FileInput';
import { UIManager } from './components/UIManager';
import { SettingsManager } from './services/SettingsManager';
import { apiService } from './services/api';
import { ipcRenderer } from 'electron';

export class App {
    private fileInput: FileInput;
    private uiManager: UIManager;
    private settingsManager: SettingsManager;

    constructor() {
        console.log('App constructor called');
        this.fileInput = new FileInput();
        this.uiManager = new UIManager();
        this.settingsManager = new SettingsManager();
        this.initializeEventListeners().catch(error => {
            console.error('Error during initializeEventListeners:', error);
            this.uiManager.showStatus(`Application initialization failed: ${error.message}`, 'error');
        });
    }

    private async initializeEventListeners(): Promise<void> {
        console.log('initializeEventListeners called');
        // Load settings
        console.log('Loading settings...');
        try {
            const settings = await this.settingsManager.loadSettings();
            console.log('Settings loaded:', settings);
            if (settings.lastUsedDirectory) {
                this.fileInput.setOutputPath(settings.lastUsedDirectory);
            }
        } catch (error) {
            console.error('Error loading settings:', error);
            // Continue even if settings fail to load initially, use default settings
        }

        console.log('Setting up browse button listeners...');
        // Browse buttons
        document.getElementById('browseCsv')?.addEventListener('click', async () => {
            try {
                const filePath = await ipcRenderer.invoke('select-csv-file');
                console.log('Selected CSV file path:', filePath);
                if (filePath) {
                    this.fileInput.setCsvPath(filePath);
                }
            } catch (error: any) {
                this.uiManager.showStatus('Error selecting CSV file', 'error');
            }
        });

        document.getElementById('browseTemplate')?.addEventListener('click', async () => {
            try {
                const filePath = await ipcRenderer.invoke('select-template-file', this.fileInput.getTemplateType());
                console.log('Selected template file path:', filePath);
                if (filePath) {
                    this.fileInput.setTemplatePath(filePath);
                }
            } catch (error: any) {
                this.uiManager.showStatus('Error selecting template file', 'error');
            }
        });

        document.getElementById('browseOutput')?.addEventListener('click', async () => {
            try {
                const dirPath = await ipcRenderer.invoke('select-output-directory');
                console.log('Selected output directory path:', dirPath);
                if (dirPath) {
                    this.fileInput.setOutputPath(dirPath);
                    await this.settingsManager.saveSettings({ lastUsedDirectory: dirPath });
                }
            } catch (error: any) {
                this.uiManager.showStatus('Error selecting output directory', 'error');
            }
        });

        // Configure file name pattern
        document.getElementById('configureFileName')?.addEventListener('click', async () => {
            const newPattern = prompt('Enter file name pattern:', this.settingsManager.getFileNamePattern());
            if (newPattern) {
                await this.settingsManager.saveSettings({ fileNamePattern: newPattern });
            }
        });

        // Preview button
        document.getElementById('preview')?.addEventListener('click', async () => {
            if (!this.fileInput.getCsvPath()) {
                this.uiManager.showStatus('Please select a CSV file first.', 'error');
                return;
            }

            try {
                const preview = await apiService.previewFileName({
                    csvFilePath: this.fileInput.getCsvPath(),
                    fileNamePattern: this.settingsManager.getFileNamePattern()
                });
                this.uiManager.showStatus(`Preview of first file name: ${preview.previewName}`, 'info');
            } catch (error: any) {
                this.uiManager.showStatus(`Error generating preview: ${error.message}`, 'error');
            }
        });

        // Process button
        document.getElementById('process')?.addEventListener('click', async () => {
            if (!this.fileInput.validate()) {
                this.uiManager.showStatus('Please fill in all required fields.', 'error');
                return;
            }

            try {
                this.uiManager.showProgressModal();
                const result = await apiService.processFiles({
                    csvFilePath: this.fileInput.getCsvPath(),
                    templatePath: this.fileInput.getTemplatePath(),
                    outputPath: this.fileInput.getOutputPath(),
                    templateType: this.fileInput.getTemplateType(),
                    fileNamePattern: this.settingsManager.getFileNamePattern()
                });

                if (result.success) {
                    this.uiManager.showProgressSummary(`Successfully processed ${result.processedFiles} files.`);
                } else {
                    this.uiManager.showStatus(`Error: ${result.errors?.join(', ')}`, 'error');
                }
            } catch (error: any) {
                this.uiManager.showStatus(`Error processing files: ${error.message}`, 'error');
            }
        });

        // Done button
        this.uiManager.setDoneButtonHandler(() => {
            this.uiManager.showStatus('Processing complete!', 'success');
        });
    }
} 
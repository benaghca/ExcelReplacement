jest.mock('electron', () => ({
    ipcRenderer: {
        invoke: jest.fn()
    }
}));
import '@jest/globals';
import { App } from '../App';
import { FileInput } from '../components/FileInput';
import { UIManager } from '../components/UIManager';
import { SettingsManager } from '../services/SettingsManager';
import { apiService } from '../services/api';
import { resetMocks } from './testUtils';

jest.mock('../components/FileInput');
jest.mock('../components/UIManager');
jest.mock('../services/SettingsManager');
jest.mock('../services/api');

describe('App', () => {
    let app: App;
    let mockFileInput: jest.Mocked<FileInput>;
    let mockUIManager: jest.Mocked<UIManager>;
    let mockSettingsManager: jest.Mocked<SettingsManager>;
    let mockElements: {
        browseCsv: HTMLButtonElement;
        browseTemplate: HTMLButtonElement;
        browseOutput: HTMLButtonElement;
        configureFileName: HTMLButtonElement;
        preview: HTMLButtonElement;
        process: HTMLButtonElement;
    };

    beforeEach(() => {
        // Reset all mocks
        resetMocks();

        // Create mock DOM elements
        mockElements = {
            browseCsv: document.createElement('button'),
            browseTemplate: document.createElement('button'),
            browseOutput: document.createElement('button'),
            configureFileName: document.createElement('button'),
            preview: document.createElement('button'),
            process: document.createElement('button')
        };

        // Setup mock getElementById
        (document.getElementById as jest.Mock).mockImplementation((id: string) => {
            switch (id) {
                case 'browseCsv': return mockElements.browseCsv;
                case 'browseTemplate': return mockElements.browseTemplate;
                case 'browseOutput': return mockElements.browseOutput;
                case 'configureFileName': return mockElements.configureFileName;
                case 'preview': return mockElements.preview;
                case 'process': return mockElements.process;
                default: return null;
            }
        });

        // Setup mock implementations
        mockFileInput = {
            getCsvPath: jest.fn(),
            getTemplatePath: jest.fn(),
            getOutputPath: jest.fn(),
            getTemplateType: jest.fn(),
            validate: jest.fn(),
            setCsvPath: jest.fn(),
            setTemplatePath: jest.fn(),
            setOutputPath: jest.fn()
        } as any;

        mockUIManager = {
            showStatus: jest.fn(),
            showProgressModal: jest.fn(),
            hideProgressModal: jest.fn(),
            updateProgress: jest.fn(),
            showProgressSummary: jest.fn(),
            setDoneButtonHandler: jest.fn()
        } as any;

        mockSettingsManager = {
            loadSettings: jest.fn().mockResolvedValue({
                fileNamePattern: '[Test] [Pattern]',
                lastUsedDirectory: '/test/dir'
            }),
            saveSettings: jest.fn(),
            getFileNamePattern: jest.fn(),
            getLastUsedDirectory: jest.fn()
        } as any;

        // Mock constructors
        (FileInput as jest.Mock).mockImplementation(() => mockFileInput);
        (UIManager as jest.Mock).mockImplementation(() => mockUIManager);
        (SettingsManager as jest.Mock).mockImplementation(() => mockSettingsManager);

        // Create app instance
        app = new App();
    });

    test('should initialize components and load settings', async () => {
        // Wait for initialization
        await new Promise(process.nextTick);

        expect(mockSettingsManager.loadSettings).toHaveBeenCalled();
        expect(mockFileInput.setOutputPath).toHaveBeenCalledWith('/test/dir');
    });

    test('should handle file selection', async () => {
        const { ipcRenderer } = require('electron');
        const mockFilePath = '/test/file.csv';
        ipcRenderer.invoke.mockResolvedValueOnce(mockFilePath);

        // Simulate browse button click
        mockElements.browseCsv.click();

        await new Promise(process.nextTick);

        expect(ipcRenderer.invoke).toHaveBeenCalledWith('select-csv-file');
        expect(mockFileInput.setCsvPath).toHaveBeenCalledWith(mockFilePath);
    });

    test('should handle file processing', async () => {
        const { ipcRenderer } = require('electron');
        // Setup mock data
        mockFileInput.validate.mockReturnValue(true);
        mockFileInput.getCsvPath.mockReturnValue('/test.csv');
        mockFileInput.getTemplatePath.mockReturnValue('/template.xlsx');
        mockFileInput.getOutputPath.mockReturnValue('/output');
        mockFileInput.getTemplateType.mockReturnValue('excel');
        mockSettingsManager.getFileNamePattern.mockReturnValue('[Test] [Pattern]');

        const mockResponse = {
            success: true,
            processedFiles: 5
        };
        (apiService.processFiles as jest.Mock).mockResolvedValueOnce(mockResponse);

        // Simulate process button click
        mockElements.process.click();

        await new Promise(process.nextTick);

        expect(mockUIManager.showProgressModal).toHaveBeenCalled();
        expect(apiService.processFiles).toHaveBeenCalledWith({
            csvFilePath: '/test.csv',
            templatePath: '/template.xlsx',
            outputPath: '/output',
            templateType: 'excel',
            fileNamePattern: '[Test] [Pattern]'
        });
        expect(mockUIManager.showProgressSummary).toHaveBeenCalledWith(
            'Successfully processed 5 files.'
        );
    });

    test('should handle processing errors', async () => {
        const { ipcRenderer } = require('electron');
        // Setup mock data
        mockFileInput.validate.mockReturnValue(true);
        (apiService.processFiles as jest.Mock).mockRejectedValueOnce(new Error('Processing failed'));

        // Simulate process button click
        mockElements.process.click();

        await new Promise(process.nextTick);

        expect(mockUIManager.showStatus).toHaveBeenCalledWith(
            'Error processing files: Processing failed',
            'error'
        );
    });
}); 
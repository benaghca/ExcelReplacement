import '@jest/globals';
import { FileInput } from '../components/FileInput';
import { resetMocks } from './testUtils';

describe('FileInput', () => {
    let fileInput: FileInput;
    let mockElements: {
        csvFile: HTMLInputElement;
        templateFile: HTMLInputElement;
        outputDir: HTMLInputElement;
        templateType: HTMLSelectElement;
    };

    beforeEach(() => {
        resetMocks();
        // Create mock DOM elements
        mockElements = {
            csvFile: document.createElement('input'),
            templateFile: document.createElement('input'),
            outputDir: document.createElement('input'),
            templateType: document.createElement('select')
        };

        // Mock getElementById
        document.getElementById = jest.fn((id: string) => {
            switch (id) {
                case 'csvFile': return mockElements.csvFile;
                case 'templateFile': return mockElements.templateFile;
                case 'outputDir': return mockElements.outputDir;
                case 'templateType': return mockElements.templateType;
                default: return null;
            }
        });

        fileInput = new FileInput();
    });

    test('should initialize with empty values', () => {
        expect(fileInput.getCsvPath()).toBe('');
        expect(fileInput.getTemplatePath()).toBe('');
        expect(fileInput.getOutputPath()).toBe('');
    });

    test('should set and get CSV path', () => {
        const testPath = 'test/path.csv';
        fileInput.setCsvPath(testPath);
        expect(fileInput.getCsvPath()).toBe(testPath);
    });

    test('should set and get template path', () => {
        const testPath = 'test/template.xlsx';
        fileInput.setTemplatePath(testPath);
        expect(fileInput.getTemplatePath()).toBe(testPath);
    });

    test('should set and get output path', () => {
        const testPath = 'test/output';
        fileInput.setOutputPath(testPath);
        expect(fileInput.getOutputPath()).toBe(testPath);
    });

    test('should validate required fields', () => {
        expect(fileInput.validate()).toBe(false);

        fileInput.setCsvPath('test.csv');
        expect(fileInput.validate()).toBe(false);

        fileInput.setTemplatePath('template.xlsx');
        expect(fileInput.validate()).toBe(false);

        fileInput.setOutputPath('output');
        expect(fileInput.validate()).toBe(true);
    });

    test('should get template type', () => {
        // Set the value directly on the element
        Object.defineProperty(mockElements.templateType, 'value', {
            writable: true,
            value: 'excel'
        });
        expect(fileInput.getTemplateType()).toBe('excel');

        Object.defineProperty(mockElements.templateType, 'value', {
            writable: true,
            value: 'word'
        });
        expect(fileInput.getTemplateType()).toBe('word');
    });
}); 
import '@jest/globals';
import { UIManager } from '../components/UIManager';
import { resetMocks } from './testUtils';

describe('UIManager', () => {
    let uiManager: UIManager;
    let mockElements: {
        statusDiv: HTMLDivElement;
        progressModal: HTMLDivElement;
        progressBar: HTMLElement;
        progressText: HTMLElement;
        currentFile: HTMLElement;
        doneButton: HTMLButtonElement;
    };

    beforeEach(() => {
        resetMocks();
        // Create mock DOM elements
        mockElements = {
            statusDiv: document.createElement('div'),
            progressModal: document.createElement('div'),
            progressBar: document.createElement('div'),
            progressText: document.createElement('div'),
            currentFile: document.createElement('div'),
            doneButton: document.createElement('button')
        };

        // Mock getElementById and querySelector
        document.getElementById = jest.fn((id: string) => {
            switch (id) {
                case 'status': return mockElements.statusDiv;
                case 'progressModal': return mockElements.progressModal;
                case 'doneButton': return mockElements.doneButton;
                default: return null;
            }
        });

        document.querySelector = jest.fn((selector: string) => {
            switch (selector) {
                case '.progress': return mockElements.progressBar;
                case '.progress-text': return mockElements.progressText;
                case '.current-file': return mockElements.currentFile;
                default: return null;
            }
        });

        uiManager = new UIManager();
    });

    test('should show status message', () => {
        const message = 'Test message';
        uiManager.showStatus(message, 'info');
        expect(mockElements.statusDiv.textContent).toBe(message);
        expect(mockElements.statusDiv.className).toBe('status info');
    });

    test('should show progress modal', () => {
        uiManager.showProgressModal();
        expect(mockElements.progressModal.style.display).toBe('flex');
    });

    test('should hide progress modal', () => {
        uiManager.hideProgressModal();
        expect(mockElements.progressModal.style.display).toBe('none');
    });

    test('should update progress', () => {
        uiManager.updateProgress(5, 10, 'test.xlsx');
        expect(mockElements.progressBar.style.width).toBe('50%');
        expect(mockElements.progressText.textContent).toBe('Processing 5 of 10 files');
        expect(mockElements.currentFile.textContent).toBe('test.xlsx');
    });

    test('should show progress summary', () => {
        const message = 'Processing complete';
        uiManager.showProgressSummary(message);
        expect(mockElements.progressText.textContent).toBe(message);
        expect(mockElements.doneButton.style.display).toBe('block');
    });

    test('should set done button handler', () => {
        const handler = jest.fn();
        uiManager.setDoneButtonHandler(handler);
        mockElements.doneButton.click();
        expect(handler).toHaveBeenCalled();
        expect(mockElements.progressModal.style.display).toBe('none');
    });
}); 
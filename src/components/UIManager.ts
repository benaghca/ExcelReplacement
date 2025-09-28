export class UIManager {
    private statusDiv: HTMLDivElement;
    private progressModal: HTMLDivElement;
    private progressBar: HTMLElement;
    private progressText: HTMLElement;
    private currentFile: HTMLElement;
    private doneButton: HTMLButtonElement;

    constructor() {
        this.statusDiv = document.getElementById('status') as HTMLDivElement;
        this.progressModal = document.getElementById('progressModal') as HTMLDivElement;
        this.progressBar = document.querySelector('.progress') as HTMLElement;
        this.progressText = document.querySelector('.progress-text') as HTMLElement;
        this.currentFile = document.querySelector('.current-file') as HTMLElement;
        this.doneButton = document.getElementById('doneButton') as HTMLButtonElement;
    }

    public showStatus(message: string, type: 'info' | 'error' | 'success' = 'info'): void {
        this.statusDiv.textContent = message;
        this.statusDiv.className = `status ${type}`;
    }

    public showProgressModal(): void {
        this.progressModal.style.display = 'flex';
    }

    public hideProgressModal(): void {
        this.progressModal.style.display = 'none';
    }

    public updateProgress(current: number, total: number, currentFileName: string): void {
        const percentage = (current / total) * 100;
        this.progressBar.style.width = `${percentage}%`;
        this.progressText.textContent = `Processing ${current} of ${total} files`;
        this.currentFile.textContent = currentFileName;
    }

    public showProgressSummary(message: string): void {
        this.progressText.textContent = message;
        this.doneButton.style.display = 'block';
    }

    public setDoneButtonHandler(handler: () => void): void {
        this.doneButton.onclick = () => {
            handler();
            this.hideProgressModal();
        };
    }
} 
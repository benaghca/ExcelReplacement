import { CsvRecord } from '../types/api';

export class FileInput {
    private csvFile: HTMLInputElement;
    private templateFile: HTMLInputElement;
    private outputDir: HTMLInputElement;
    private templateType: HTMLSelectElement;

    constructor() {
        this.csvFile = document.getElementById('csvFile') as HTMLInputElement;
        this.templateFile = document.getElementById('templateFile') as HTMLInputElement;
        this.outputDir = document.getElementById('outputDir') as HTMLInputElement;
        this.templateType = document.getElementById('templateType') as HTMLSelectElement;
    }

    public getCsvPath(): string {
        return this.csvFile.value;
    }

    public getTemplatePath(): string {
        return this.templateFile.value;
    }

    public getOutputPath(): string {
        return this.outputDir.value;
    }

    public getTemplateType(): 'excel' | 'word' {
        return this.templateType.value as 'excel' | 'word';
    }

    public validate(): boolean {
        if (!this.csvFile.value || !this.templateFile.value || !this.outputDir.value) {
            return false;
        }
        return true;
    }

    public setCsvPath(path: string): void {
        this.csvFile.value = path;
    }

    public setTemplatePath(path: string): void {
        this.templateFile.value = path;
    }

    public setOutputPath(path: string): void {
        this.outputDir.value = path;
    }
} 
export interface ProcessRequest {
    csvFilePath: string;
    templatePath: string;
    outputPath: string;
    templateType: 'excel' | 'word';
    fileNamePattern: string;
}

export interface PreviewRequest {
    csvFilePath: string;
    fileNamePattern: string;
}

export interface ProcessResponse {
    success: boolean;
    processedFiles: number;
    outputFiles?: string[];
    errors?: string[];
}

export interface PreviewResponse {
    previewName: string;
    fields: Record<string, string>;
}

export interface ApiError {
    error: string;
}

// Frontend specific types
export interface Settings {
    fileNamePattern: string;
    lastUsedDirectory: string;
}

export interface CsvRecord {
    Location?: string;
    Facility?: string;
    Equipment?: string;
    Procedure?: string;
    [key: string]: string | undefined;
} 
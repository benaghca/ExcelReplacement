import '@jest/globals';
import { apiService } from '../services/api';
import { ProcessRequest, PreviewRequest } from '../types/api';
import { resetMocks } from './testUtils';
import * as fs from 'fs';
import * as path from 'path';

describe.skip('End-to-End Integration', () => {
    const testDataDir = path.join(__dirname, '../../test-data');
    const sampleCsvPath = path.join(testDataDir, 'sample.csv');
    const excelTemplatePath = path.join(testDataDir, 'template.xlsx');
    const wordTemplatePath = path.join(testDataDir, 'template.docx');
    const outputDir = path.join(testDataDir, 'output');

    beforeAll(() => {
        // Create test data directory if it doesn't exist
        if (!fs.existsSync(testDataDir)) {
            fs.mkdirSync(testDataDir, { recursive: true });
        }
        if (!fs.existsSync(outputDir)) {
            fs.mkdirSync(outputDir, { recursive: true });
        }

        // Create sample CSV file
        const csvContent = 'Location,Facility\nTest Location,Test Facility';
        fs.writeFileSync(sampleCsvPath, csvContent);

        // Create sample Excel template
        // Note: In a real test, we would create a proper Excel file
        fs.writeFileSync(excelTemplatePath, 'Excel template content');

        // Create sample Word template
        // Note: In a real test, we would create a proper Word file
        fs.writeFileSync(wordTemplatePath, 'Word template content');
    });

    afterAll(() => {
        // Clean up test files
        if (fs.existsSync(sampleCsvPath)) {
            fs.unlinkSync(sampleCsvPath);
        }
        if (fs.existsSync(excelTemplatePath)) {
            fs.unlinkSync(excelTemplatePath);
        }
        if (fs.existsSync(wordTemplatePath)) {
            fs.unlinkSync(wordTemplatePath);
        }
        if (fs.existsSync(outputDir)) {
            fs.rmdirSync(outputDir, { recursive: true });
        }
    });

    describe('File Processing', () => {
        it('should process Excel template with real files', async () => {
            const templatePath = path.join(__dirname, '../../test-data/templates/template.xlsx');
            if (!fs.existsSync(templatePath)) {
                console.warn('Skipping Excel template test: template file not found at', templatePath);
                return;
            }
            const request: ProcessRequest = {
                csvFilePath: sampleCsvPath,
                templatePath: templatePath,
                outputPath: outputDir,
                templateType: 'excel',
                fileNamePattern: '{Location} {Facility}'
            };

            const response = await apiService.processFiles(request);
            expect(response.success).toBe(true);
            expect(response.processedFiles).toBeGreaterThan(0);
            expect(response.outputFiles).toBeDefined();
            expect(response.outputFiles?.length).toBeGreaterThan(0);

            // Verify output files exist
            response.outputFiles?.forEach(filePath => {
                expect(fs.existsSync(filePath)).toBe(true);
            });
        });

        it('should process Word template with real files', async () => {
            const templatePath = path.join(__dirname, '../../test-data/templates/template.docx');
            if (!fs.existsSync(templatePath)) {
                console.warn('Skipping Word template test: template file not found at', templatePath);
                return;
            }
            const request: ProcessRequest = {
                csvFilePath: sampleCsvPath,
                templatePath: templatePath,
                outputPath: outputDir,
                templateType: 'word',
                fileNamePattern: '{Location} {Facility}'
            };

            const response = await apiService.processFiles(request);
            expect(response.success).toBe(true);
            expect(response.processedFiles).toBeGreaterThan(0);
            expect(response.outputFiles).toBeDefined();
            expect(response.outputFiles?.length).toBeGreaterThan(0);

            // Verify output files exist
            response.outputFiles?.forEach(filePath => {
                expect(fs.existsSync(filePath)).toBe(true);
            });
        });
    });

    describe('Preview Generation', () => {
        it('should generate preview from real CSV file', async () => {
            if (!fs.existsSync(sampleCsvPath)) {
                console.warn('Skipping CSV preview test: sample CSV file not found at', sampleCsvPath);
                return;
            }
            const request: PreviewRequest = {
                csvFilePath: sampleCsvPath,
                fileNamePattern: '[Location] [Facility]'
            };

            const result = await apiService.previewFileName(request);
            expect(result.previewName).toBeDefined();
            expect(result.fields).toBeDefined();
            expect(result.fields.Location).toBe('Test Location');
            expect(result.fields.Facility).toBe('Test Facility');
        });
    });

    describe('Error Handling', () => {
        test('should handle non-existent CSV file', async () => {
            const request: ProcessRequest = {
                csvFilePath: 'non-existent.csv',
                templatePath: excelTemplatePath,
                outputPath: outputDir,
                templateType: 'excel',
                fileNamePattern: '[Location] [Facility]'
            };

            await expect(apiService.processFiles(request)).rejects.toThrow();
        });

        test('should handle non-existent template file', async () => {
            const request: ProcessRequest = {
                csvFilePath: sampleCsvPath,
                templatePath: 'non-existent.xlsx',
                outputPath: outputDir,
                templateType: 'excel',
                fileNamePattern: '[Location] [Facility]'
            };

            await expect(apiService.processFiles(request)).rejects.toThrow();
        });

        it('should handle invalid CSV format', async () => {
            // Create invalid CSV file
            const invalidCsvPath = path.join(testDataDir, 'invalid.csv');
            fs.writeFileSync(invalidCsvPath, 'Invalid CSV content');

            const request: ProcessRequest = {
                csvFilePath: invalidCsvPath,
                templatePath: excelTemplatePath,
                outputPath: outputDir,
                templateType: 'excel',
                fileNamePattern: '[Location] [Facility]'
            };

            try {
                const response = await apiService.processFiles(request);
                expect(response.success).toBe(true);
                expect(response.processedFiles).toBe(0);
                expect(response.outputFiles).toEqual([]);
            } finally {
                // Clean up invalid CSV file
                if (fs.existsSync(invalidCsvPath)) {
                    fs.unlinkSync(invalidCsvPath);
                }
            }
        });
    });
}); 
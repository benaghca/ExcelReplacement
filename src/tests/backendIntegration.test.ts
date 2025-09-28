import '@jest/globals';
import { apiService } from '../services/api';
import { ProcessRequest, PreviewRequest } from '../types/api';
import { resetMocks } from './testUtils';

describe('C# Backend Integration', () => {
    beforeEach(() => {
        resetMocks();
    });

    describe('File Processing', () => {
        const mockExcelRequest: ProcessRequest = {
            csvFilePath: 'test.csv',
            templatePath: 'template.xlsx',
            outputPath: 'output',
            templateType: 'excel',
            fileNamePattern: '[Test] [Pattern]'
        };

        const mockWordRequest: ProcessRequest = {
            csvFilePath: 'test.csv',
            templatePath: 'template.docx',
            outputPath: 'output',
            templateType: 'word',
            fileNamePattern: '[Test] [Pattern]'
        };

        test('should process Excel template successfully', async () => {
            const mockResponse = {
                success: true,
                processedFiles: 5,
                outputFiles: [
                    'output/Test1.xlsx',
                    'output/Test2.xlsx',
                    'output/Test3.xlsx',
                    'output/Test4.xlsx',
                    'output/Test5.xlsx'
                ]
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockResponse)
            });

            const result = await apiService.processFiles(mockExcelRequest);
            expect(result).toEqual(mockResponse);
            expect(global.fetch).toHaveBeenCalledWith(
                'http://localhost:5000/api/fileprocessing/process',
                {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(mockExcelRequest)
                }
            );
        });

        test('should process Word template successfully', async () => {
            const mockResponse = {
                success: true,
                processedFiles: 3,
                outputFiles: [
                    'output/Test1.docx',
                    'output/Test2.docx',
                    'output/Test3.docx'
                ]
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockResponse)
            });

            const result = await apiService.processFiles(mockWordRequest);
            expect(result).toEqual(mockResponse);
        });

        test('should handle invalid template type', async () => {
            const invalidRequest = {
                ...mockExcelRequest,
                templateType: 'invalid' as any
            };

            const mockError = {
                success: false,
                error: 'Unsupported file type. Only \'excel\' and \'word\' are supported.'
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: false,
                json: () => Promise.resolve(mockError)
            });

            await expect(apiService.processFiles(invalidRequest)).rejects.toThrow(
                'Unsupported file type. Only \'excel\' and \'word\' are supported.'
            );
        });

        test('should handle missing template file', async () => {
            const mockError = {
                success: false,
                error: 'Template file not found'
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: false,
                json: () => Promise.resolve(mockError)
            });

            await expect(apiService.processFiles(mockExcelRequest)).rejects.toThrow('Template file not found');
        });
    });

    describe('Preview Generation', () => {
        const mockPreviewRequest: PreviewRequest = {
            csvFilePath: 'test.csv',
            fileNamePattern: '[Test] [Pattern]'
        };

        test('should generate preview for Excel template', async () => {
            const mockResponse = {
                previewName: 'Test Pattern.xlsx',
                fields: {
                    Location: 'Test',
                    Facility: 'Pattern'
                },
                templateType: 'excel'
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockResponse)
            });

            const result = await apiService.previewFileName(mockPreviewRequest);
            expect(result).toEqual(mockResponse);
        });

        test('should generate preview for Word template', async () => {
            const mockResponse = {
                previewName: 'Test Pattern.docx',
                fields: {
                    Location: 'Test',
                    Facility: 'Pattern'
                },
                templateType: 'word'
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: true,
                json: () => Promise.resolve(mockResponse)
            });

            const result = await apiService.previewFileName(mockPreviewRequest);
            expect(result).toEqual(mockResponse);
        });

        test('should handle invalid CSV format', async () => {
            const mockError = {
                success: false,
                error: 'Invalid CSV format'
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: false,
                json: () => Promise.resolve(mockError)
            });

            await expect(apiService.previewFileName(mockPreviewRequest)).rejects.toThrow('Invalid CSV format');
        });

        test('should handle missing CSV file', async () => {
            const mockError = {
                success: false,
                error: 'CSV file not found'
            };

            (global.fetch as jest.Mock).mockResolvedValueOnce({
                ok: false,
                json: () => Promise.resolve(mockError)
            });

            await expect(apiService.previewFileName(mockPreviewRequest)).rejects.toThrow('CSV file not found');
        });
    });

    describe('Error Handling', () => {
        test('should handle backend server unavailable', async () => {
            (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Failed to connect to backend server'));
            
            const request: ProcessRequest = {
                csvFilePath: 'test.csv',
                templatePath: 'template.xlsx',
                outputPath: 'output',
                templateType: 'excel',
                fileNamePattern: '[Test] [Pattern]'
            };
            
            await expect(apiService.processFiles(request)).rejects.toThrow('Failed to connect to backend server');
        });

        test('should handle timeout', async () => {
            (global.fetch as jest.Mock).mockRejectedValueOnce(new Error('Request timeout'));
            
            const request: ProcessRequest = {
                csvFilePath: 'test.csv',
                templatePath: 'template.xlsx',
                outputPath: 'output',
                templateType: 'excel',
                fileNamePattern: '[Test] [Pattern]'
            };
            
            await expect(apiService.processFiles(request)).rejects.toThrow('Request timeout');
        });
    });
}); 
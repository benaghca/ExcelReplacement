import '@jest/globals';
import { TextEncoder, TextDecoder } from 'util';
import fetch from 'node-fetch';

// Add fetch to global
global.fetch = fetch as any;

// Add TextEncoder/TextDecoder to global
global.TextEncoder = TextEncoder;
global.TextDecoder = TextDecoder as any;

// Mock electron
jest.mock('electron', () => ({
    ipcRenderer: {
        invoke: jest.fn()
    }
}));

// Reset all mocks before each test
beforeEach(() => {
    jest.clearAllMocks();
}); 
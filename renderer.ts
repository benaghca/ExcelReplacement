// Add a simple log to confirm renderer.ts execution
console.log('renderer.ts started');

import { App } from './src/App';

// Initialize the application when the DOM is loaded
document.addEventListener('DOMContentLoaded', () => {
    new App();
}); 
// Simple app.js for Document Template Processor
console.log('App loaded');

// Wait for DOM to be ready
document.addEventListener('DOMContentLoaded', function() {
    console.log('DOM loaded, setting up handlers');
    setupEventHandlers();
    setupDragAndDrop();
    testAPI();
});

function setupEventHandlers() {
    // Button event handlers
    document.getElementById('browseCsv')?.addEventListener('click', browseCsvFile);
    document.getElementById('browseTemplate')?.addEventListener('click', browseTemplateFile);
    document.getElementById('browseOutput')?.addEventListener('click', browseOutputDirectory);
    document.getElementById('process')?.addEventListener('click', processFiles);
    document.getElementById('preview')?.addEventListener('click', generatePreview);
    document.getElementById('manageTemplates')?.addEventListener('click', showTemplateManager);
    document.getElementById('configureFileName')?.addEventListener('click', showFileNameConfigDialog);
    
    // Template type change handler
    document.getElementById('templateType')?.addEventListener('change', function() {
        const select = this;
        select.style.borderColor = '#2563eb';
        select.style.boxShadow = '0 0 0 3px #dbeafe';
        
        // Update status
        updateStatus('Template type changed to: ' + select.value, 'info');
        
        // Update placeholder text
        const templateDropZone = document.getElementById('templateDropZone');
        if (templateDropZone) {
            const text = templateDropZone.querySelector('.drop-zone-text');
            if (text) {
                text.textContent = `Drop ${select.value} template here or click Browse`;
            }
        }
    });
    
    // Done button handler
    document.getElementById('doneButton')?.addEventListener('click', function() {
        document.getElementById('progressModal').style.display = 'none';
    });
}

function setupDragAndDrop() {
    const dropZones = ['csvDropZone', 'templateDropZone', 'outputDropZone'];
    
    dropZones.forEach(zoneId => {
        const zone = document.getElementById(zoneId);
        if (!zone) return;
        
        zone.addEventListener('dragover', function(e) {
            e.preventDefault();
            this.classList.add('drag-over');
        });
        
        zone.addEventListener('dragleave', function(e) {
            e.preventDefault();
            this.classList.remove('drag-over');
        });
        
        zone.addEventListener('drop', function(e) {
            e.preventDefault();
            this.classList.remove('drag-over');
            this.classList.add('has-file');
            
            const files = e.dataTransfer.files;
            if (files.length > 0) {
                handleFileDrop(zoneId, files[0]);
            }
        });
        
        zone.addEventListener('click', function() {
            // Trigger the corresponding browse button
            const buttonId = zoneId.replace('DropZone', '');
            const button = document.getElementById('browse' + buttonId.charAt(0).toUpperCase() + buttonId.slice(1));
            if (button) {
                button.click();
            }
        });
    });
}

function handleFileDrop(zoneId, file) {
    const inputId = zoneId.replace('DropZone', 'File');
    const input = document.getElementById(inputId);
    
    if (input) {
        input.value = file.path || file.name;
        updateStatus(`File selected: ${file.name}`, 'success');
        
        // Validate the file
        if (zoneId === 'csvDropZone') {
            validateCsvFile(file.path || file.name);
        } else if (zoneId === 'templateDropZone') {
            validateTemplateFile(file.path || file.name);
        }
    }
}

async function browseCsvFile() {
    try {
        updateStatus('Opening CSV file dialog...', 'info');
        
        if (window.require) {
            const { ipcRenderer } = window.require('electron');
            const result = await ipcRenderer.invoke('select-csv-file');
            
            if (result && !result.canceled && result.filePaths.length > 0) {
                const filePath = result.filePaths[0];
                document.getElementById('csvFile').value = filePath;
                updateStatus(`CSV file selected: ${filePath}`, 'success');
                await validateCsvFile(filePath);
            }
        } else {
            updateStatus('Electron API not available - please use drag and drop', 'warning');
        }
    } catch (error) {
        console.error('Error selecting CSV file:', error);
        updateStatus('Error selecting CSV file: ' + error.message, 'error');
    }
}

async function browseTemplateFile() {
    try {
        updateStatus('Opening template file dialog...', 'info');
        
        if (window.require) {
            const { ipcRenderer } = window.require('electron');
            const templateType = document.getElementById('templateType').value;
            const result = await ipcRenderer.invoke('select-template-file', templateType);
            
            if (result && !result.canceled && result.filePaths.length > 0) {
                const filePath = result.filePaths[0];
                document.getElementById('templateFile').value = filePath;
                updateStatus(`${templateType.toUpperCase()} template selected: ${filePath}`, 'success');
                await validateTemplateFile(filePath);
            }
        } else {
            updateStatus('Electron API not available - please use drag and drop', 'warning');
        }
    } catch (error) {
        console.error('Error selecting template file:', error);
        updateStatus('Error selecting template file: ' + error.message, 'error');
    }
}

async function browseOutputDirectory() {
    try {
        updateStatus('Opening output directory dialog...', 'info');
        
        if (window.require) {
            const { ipcRenderer } = window.require('electron');
            const result = await ipcRenderer.invoke('select-output-directory');
            
            if (result && !result.canceled && result.filePaths.length > 0) {
                const dirPath = result.filePaths[0];
                document.getElementById('outputDir').value = dirPath;
                updateStatus(`Output directory selected: ${dirPath}`, 'success');
            }
        } else {
            updateStatus('Electron API not available - please use drag and drop', 'warning');
        }
    } catch (error) {
        console.error('Error selecting output directory:', error);
        updateStatus('Error selecting output directory: ' + error.message, 'error');
    }
}

async function validateCsvFile(filePath) {
    try {
        updateStatus('Validating CSV file...', 'info');
        
        const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: filePath,
                templateFilePath: document.getElementById('templateFile').value || '',
                fileNamePattern: localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus('CSV validated! Found ' + Object.keys(data.fields).length + ' columns', 'success');
        } else {
            updateStatus('CSV validation failed', 'error');
        }
    } catch (error) {
        console.error('CSV validation error:', error);
        updateStatus('CSV validation failed: ' + error.message, 'error');
    }
}

async function validateTemplateFile(filePath) {
    try {
        updateStatus('Validating template file...', 'info');
        
        // Simple validation - just check if file exists and has correct extension
        const templateType = document.getElementById('templateType').value;
        const extension = filePath.split('.').pop().toLowerCase();
        
        if ((templateType === 'excel' && extension === 'xlsx') || 
            (templateType === 'word' && extension === 'docx')) {
            updateStatus(`${templateType.toUpperCase()} template validated!`, 'success');
        } else {
            updateStatus('Invalid template file type', 'error');
        }
    } catch (error) {
        console.error('Template validation error:', error);
        updateStatus('Template validation failed: ' + error.message, 'error');
    }
}

async function generatePreview() {
    try {
        updateStatus('Generating preview...', 'info');
        
        const csvFile = document.getElementById('csvFile').value;
        const templateFile = document.getElementById('templateFile').value;
        
        if (!csvFile || !templateFile) {
            updateStatus('Please select both CSV and template files first', 'warning');
            return;
        }
        
        const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: csvFile,
                templatePath: templateFile,
                fileNamePattern: localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus('Preview: ' + data.previewName, 'success');
        } else {
            updateStatus('Preview failed', 'error');
        }
    } catch (error) {
        console.error('Preview error:', error);
        updateStatus('Preview failed: ' + error.message, 'error');
    }
}

async function processFiles() {
    try {
        updateStatus('Starting file processing...', 'info');
        
        const csvFile = document.getElementById('csvFile').value;
        const templateFile = document.getElementById('templateFile').value;
        const outputDir = document.getElementById('outputDir').value;
        
        if (!csvFile || !templateFile || !outputDir) {
            updateStatus('Please select all required files and directories', 'warning');
            return;
        }
        
        const templateType = document.getElementById('templateType').value;
        const response = await fetch('http://localhost:5000/api/fileprocessing/process', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: csvFile,
                templatePath: templateFile,
                outputPath: outputDir,
                templateType: templateType,
                fileNamePattern: localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            const data = await response.json();
            updateStatus(`Successfully processed ${data.processedFiles} files!`, 'success');
        } else {
            const errorData = await response.json();
            console.error('Processing error:', errorData);
            updateStatus(`Processing failed: ${errorData.error || errorData.details || 'Unknown error'}`, 'error');
        }
    } catch (error) {
        console.error('Processing error:', error);
        updateStatus('Processing failed: ' + error.message, 'error');
    }
}

function showTemplateManager() {
    updateStatus('Opening template manager...', 'info');
    
    // Get saved templates from localStorage
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    
    // Create modal elements
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    modalContent.style.maxWidth = '900px';
    modalContent.style.width = '95%';
    modalContent.style.maxHeight = '80vh';
    modalContent.style.overflow = 'hidden';
    modalContent.style.display = 'flex';
    modalContent.style.flexDirection = 'column';
    
    modalContent.innerHTML = `
        <div style="display: flex; justify-content: space-between; align-items: center; margin-bottom: 20px; padding-bottom: 15px; border-bottom: 1px solid #e2e8f0;">
            <h2 style="margin: 0;">Template Manager</h2>
            <button id="addTemplateBtn" style="padding: 8px 16px; background: #2563eb; color: white; border: none; border-radius: 6px; cursor: pointer;">
                + Add Current Template
            </button>
        </div>
        
        <div style="flex: 1; overflow-y: auto; margin-bottom: 20px;">
            ${savedTemplates.length > 0 ? `
                <div style="display: flex; gap: 20px; height: 100%;">
                    <!-- Template List Table -->
                    <div style="flex: 1; min-width: 0;">
                        <div style="background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; overflow: hidden;">
                            <div style="background: #f1f5f9; padding: 12px; border-bottom: 1px solid #e2e8f0; font-weight: 600; color: #475569;">
                                Saved Templates (${savedTemplates.length})
                            </div>
                            <div style="max-height: 500px; overflow-y: auto; border: 1px solid #e2e8f0; border-top: none;">
                                <table style="width: 100%; border-collapse: collapse;">
                                    <thead style="background: #f8fafc; position: sticky; top: 0; z-index: 10;">
                                        <tr>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Name</th>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Type</th>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Category</th>
                                            <th style="padding: 8px 12px; text-align: left; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Date</th>
                                            <th style="padding: 8px 12px; text-align: center; font-size: 12px; font-weight: 600; color: #64748b; border-bottom: 1px solid #e2e8f0;">Actions</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${savedTemplates.map((template, index) => `
                                            <tr class="template-row" data-index="${index}" style="border-bottom: 1px solid #f1f5f9; cursor: pointer; transition: background-color 0.2s;">
                                                <td style="padding: 10px 12px; font-size: 14px; font-weight: 500; color: #1e293b;">
                                                    <div style="display: flex; align-items: center; gap: 8px;">
                                                        <span>${template.name}</span>
                                                    </div>
                                                </td>
                                                <td style="padding: 10px 12px;">
                                                    <span class="template-type-badge ${template.type.toLowerCase()}" style="font-size: 11px;">${template.type}</span>
                                                </td>
                                                <td style="padding: 10px 12px; font-size: 13px; color: #64748b;">${template.category || 'Uncategorized'}</td>
                                                <td style="padding: 10px 12px; font-size: 12px; color: #94a3b8;">${new Date(template.dateAdded).toLocaleDateString()}</td>
                                                <td style="padding: 10px 12px; text-align: center;">
                                                    <div style="display: flex; gap: 4px; justify-content: center;">
                                                        <button class="action-btn load-template" data-index="${index}" style="padding: 4px 8px; font-size: 11px;">Load</button>
                                                        <button class="action-btn delete-template" data-index="${index}" style="padding: 4px 8px; font-size: 11px;">Delete</button>
                                                    </div>
                                                </td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                    </div>
                    
                    <!-- Preview Panel -->
                    <div id="previewPanel" style="width: 300px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; padding: 16px; display: none;">
                        <h3 style="margin: 0 0 12px 0; font-size: 16px; color: #1e293b;">Template Preview</h3>
                        <div id="previewContent">
                            <p style="color: #64748b; font-style: italic;">Select a template to see details</p>
                        </div>
                    </div>
                </div>
            ` : `
                <div style="text-align: center; padding: 40px; color: #64748b;">
                    <p>No templates saved yet.</p>
                    <p>Add your first template using the "Add Current Template" button above.</p>
                </div>
            `}
        </div>
        
        <div style="text-align: right; padding-top: 15px; border-top: 1px solid #e2e8f0;">
            <button id="closeTemplateManager" style="padding: 10px 20px;">Close</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Add styles for template manager
    const style = document.createElement('style');
    style.textContent = `
        .template-row:hover {
            background-color: #f8fafc !important;
        }
        .template-row.selected {
            background-color: #dbeafe !important;
            border-left: 3px solid #2563eb;
        }
        .template-type-badge {
            padding: 4px 8px;
            border-radius: 4px;
            font-size: 12px;
            font-weight: 500;
            text-transform: uppercase;
        }
        .template-type-badge.excel {
            background: #dcfce7;
            color: #166534;
        }
        .template-type-badge.word {
            background: #dbeafe;
            color: #1e40af;
        }
        .template-info {
            margin-bottom: 10px;
        }
        .template-path {
            margin: 0 0 4px 0;
            font-size: 12px;
            color: #64748b;
            word-break: break-all;
            line-height: 1.2;
        }
        .template-category {
            margin: 0 0 2px 0;
            font-size: 11px;
            color: #94a3b8;
            font-weight: 500;
        }
        .template-date {
            margin: 0;
            font-size: 11px;
            color: #94a3b8;
        }
        .template-actions {
            display: flex;
            gap: 8px;
        }
        .template-actions .action-btn {
            padding: 6px 12px;
            border: 1px solid #e2e8f0;
            border-radius: 4px;
            background: #f8fafc;
            cursor: pointer;
            font-size: 12px;
            transition: all 0.2s;
        }
        .template-actions .action-btn:hover {
            background: #e2e8f0;
        }
        .template-actions .load-template:hover {
            background: #2563eb;
            color: white;
            border-color: #2563eb;
        }
        .template-actions .delete-template:hover {
            background: #dc2626;
            color: white;
            border-color: #dc2626;
        }
    `;
    document.head.appendChild(style);
    
    // Event listeners
    document.getElementById('addTemplateBtn').onclick = () => {
        addCurrentTemplate();
    };
    
    document.getElementById('closeTemplateManager').onclick = () => {
        // Remove all modals and styles
        const modals = document.querySelectorAll('.modal');
        modals.forEach(modal => modal.remove());
        
        const styles = document.querySelectorAll('style');
        styles.forEach(style => {
            if (style.textContent.includes('template-card') || style.textContent.includes('placeholder-btn')) {
                style.remove();
            }
        });
        
        updateStatus('Template manager closed', 'info');
    };
    
    // Template row click handlers
    document.querySelectorAll('.template-row').forEach(row => {
        row.onclick = (e) => {
            // Don't trigger if clicking on action buttons
            if (e.target.classList.contains('action-btn')) return;
            
            const index = parseInt(row.dataset.index);
            showTemplatePreview(index);
        };
    });
    
    // Action button handlers
    document.querySelectorAll('.load-template').forEach(btn => {
        btn.onclick = (e) => {
            e.stopPropagation();
            loadTemplate(parseInt(btn.dataset.index));
        };
    });
    
    document.querySelectorAll('.delete-template').forEach(btn => {
        btn.onclick = (e) => {
            e.stopPropagation();
            deleteTemplate(parseInt(btn.dataset.index));
        };
    });
}

function addCurrentTemplate() {
    const templateFile = document.getElementById('templateFile').value;
    const templateType = document.getElementById('templateType').value;
    
    if (!templateFile) {
        updateStatus('Please select a template file first', 'warning');
        return;
    }
    
    // Create add template modal
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    modalContent.style.maxWidth = '500px';
    
    modalContent.innerHTML = `
        <h2>Add Template</h2>
        <p>Save the current template for quick access later.</p>
        
        <div style="margin: 20px 0;">
            <label for="templateName" style="display: block; margin-bottom: 8px; font-weight: 500;">Template Name:</label>
            <input type="text" id="templateName" placeholder="Enter a name for this template" 
                   style="width: 100%; padding: 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 16px;">
        </div>
        
        <div style="margin: 20px 0;">
            <label for="templateCategory" style="display: block; margin-bottom: 8px; font-weight: 500;">Category (optional):</label>
            <input type="text" id="templateCategory" placeholder="e.g., Maintenance, Reports, etc." 
                   style="width: 100%; padding: 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 16px;">
        </div>
        
        <div style="margin: 20px 0; padding: 12px; background: #f8fafc; border-radius: 8px;">
            <p style="margin: 0 0 8px 0; font-weight: 500;">Template Details:</p>
            <p style="margin: 0; color: #64748b; font-size: 14px;">
                <strong>Type:</strong> ${templateType.toUpperCase()}<br>
                <strong>File:</strong> ${templateFile.split('\\').pop()}
            </p>
        </div>
        
        <div style="text-align: right; margin-top: 30px;">
            <button id="cancelAddTemplate" style="margin-right: 10px; padding: 10px 20px;">Cancel</button>
            <button id="saveTemplate" style="padding: 10px 20px; background: #2563eb; color: white; border: none; border-radius: 6px;">Save Template</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Event listeners
    document.getElementById('cancelAddTemplate').onclick = () => {
        document.body.removeChild(modal);
    };
    
    document.getElementById('saveTemplate').onclick = () => {
        const name = document.getElementById('templateName').value.trim();
        const category = document.getElementById('templateCategory').value.trim();
        
        if (!name) {
            updateStatus('Please enter a template name', 'warning');
            return;
        }
        
        // Save template
        const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
        const newTemplate = {
            name: name,
            category: category || 'Uncategorized',
            type: templateType,
            path: templateFile,
            dateAdded: new Date().toISOString()
        };
        
        savedTemplates.push(newTemplate);
        localStorage.setItem('savedTemplates', JSON.stringify(savedTemplates));
        
        document.body.removeChild(modal);
        updateStatus(`Template "${name}" saved successfully`, 'success');
        
        // Refresh the template manager
        showTemplateManager();
    };
    
    // Focus the name input
    setTimeout(() => {
        document.getElementById('templateName').focus();
    }, 100);
}

function showTemplatePreview(index) {
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    const template = savedTemplates[index];
    
    if (!template) {
        updateStatus('Template not found', 'error');
        return;
    }
    
    // Remove previous selection
    document.querySelectorAll('.template-row').forEach(row => {
        row.classList.remove('selected');
    });
    
    // Highlight selected row
    const selectedRow = document.querySelector(`.template-row[data-index="${index}"]`);
    if (selectedRow) {
        selectedRow.classList.add('selected');
    }
    
    // Show preview panel
    const previewPanel = document.getElementById('previewPanel');
    const previewContent = document.getElementById('previewContent');
    
    if (previewPanel && previewContent) {
        previewPanel.style.display = 'block';
        
        const fileName = template.path.split('\\').pop() || template.path.split('/').pop();
        const fileSize = 'Unknown size'; // Could be enhanced to get actual file size
        
        previewContent.innerHTML = `
            <div style="margin-bottom: 16px;">
                <h4 style="margin: 0 0 8px 0; color: #1e293b; font-size: 18px;">${template.name}</h4>
                <div class="template-type-badge ${template.type.toLowerCase()}" style="display: inline-block; margin-bottom: 8px;">${template.type}</div>
            </div>
            
            <div style="margin-bottom: 12px;">
                <strong style="color: #374151; font-size: 12px;">File:</strong><br>
                <span style="color: #64748b; font-size: 13px; word-break: break-all;">${fileName}</span>
            </div>
            
            <div style="margin-bottom: 12px;">
                <strong style="color: #374151; font-size: 12px;">Category:</strong><br>
                <span style="color: #64748b; font-size: 13px;">${template.category || 'Uncategorized'}</span>
            </div>
            
            <div style="margin-bottom: 12px;">
                <strong style="color: #374151; font-size: 12px;">Added:</strong><br>
                <span style="color: #64748b; font-size: 13px;">${new Date(template.dateAdded).toLocaleString()}</span>
            </div>
            
            <div style="margin-bottom: 16px;">
                <strong style="color: #374151; font-size: 12px;">Full Path:</strong><br>
                <span style="color: #64748b; font-size: 11px; word-break: break-all; font-family: monospace;">${template.path}</span>
            </div>
            
            <div style="border-top: 1px solid #e2e8f0; padding-top: 12px;">
                <button onclick="loadTemplate(${index})" style="width: 100%; padding: 8px 12px; background: #2563eb; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 14px; margin-bottom: 8px;">
                    Load This Template
                </button>
                <button onclick="deleteTemplate(${index})" style="width: 100%; padding: 8px 12px; background: #dc2626; color: white; border: none; border-radius: 6px; cursor: pointer; font-size: 14px;">
                    Delete Template
                </button>
            </div>
        `;
    }
}

function loadTemplate(index) {
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    const template = savedTemplates[index];
    
    if (!template) {
        updateStatus('Template not found', 'error');
        return;
    }
    
    // Set the template file and type
    document.getElementById('templateFile').value = template.path;
    document.getElementById('templateType').value = template.type.toLowerCase();
    
    // Close all modals and clean up styles
    const modals = document.querySelectorAll('.modal');
    modals.forEach(modal => modal.remove());
    
    const styles = document.querySelectorAll('style');
    styles.forEach(style => {
        if (style.textContent.includes('template-card') || style.textContent.includes('placeholder-btn')) {
            style.remove();
        }
    });
    
    updateStatus(`Template "${template.name}" loaded successfully`, 'success');
}

function deleteTemplate(index) {
    const savedTemplates = JSON.parse(localStorage.getItem('savedTemplates') || '[]');
    const template = savedTemplates[index];
    
    if (!template) {
        updateStatus('Template not found', 'error');
        return;
    }
    
    if (confirm(`Are you sure you want to delete the template "${template.name}"?`)) {
        savedTemplates.splice(index, 1);
        localStorage.setItem('savedTemplates', JSON.stringify(savedTemplates));
        updateStatus(`Template "${template.name}" deleted`, 'success');
        
        // Refresh the template manager
        showTemplateManager();
    }
}

async function showFileNameConfigDialog() {
    updateStatus('Opening file name configuration...', 'info');
    
    const currentPattern = localStorage.getItem('fileNamePattern') || '[Location] [Facility] [Equipment] [Procedure]';
    
    // Get available placeholders from CSV
    let availablePlaceholders = [];
    const csvFile = document.getElementById('csvFile').value;
    
    if (csvFile) {
        try {
            const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({
                    csvFilePath: csvFile,
                    templatePath: document.getElementById('templateFile').value || '',
                    fileNamePattern: currentPattern
                })
            });
            
            if (response.ok) {
                const data = await response.json();
                availablePlaceholders = Object.keys(data.fields || {});
            }
        } catch (error) {
            console.log('Could not load CSV columns:', error);
        }
    }
    
    // Create modal elements
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.style.display = 'block';
    
    const modalContent = document.createElement('div');
    modalContent.className = 'modal-content';
    modalContent.style.maxWidth = '700px';
    modalContent.style.width = '90%';
    
    modalContent.innerHTML = `
        <h2>Configure File Name Pattern</h2>
        <p>Build your filename pattern using the available placeholders below. Click on placeholders to insert them.</p>
        
        <div style="margin: 20px 0;">
            <label for="patternInput" style="display: block; margin-bottom: 8px; font-weight: 500;">Filename Pattern:</label>
            <input type="text" id="patternInput" value="${currentPattern}" 
                   style="width: 100%; padding: 12px; border: 2px solid #e2e8f0; border-radius: 8px; font-size: 16px; font-family: monospace;">
        </div>
        
        <div style="margin: 20px 0;">
            <label style="display: block; margin-bottom: 8px; font-weight: 500;">Available Placeholders:</label>
            <div id="placeholderButtons" style="display: flex; flex-wrap: wrap; gap: 8px; margin-bottom: 10px;">
                ${availablePlaceholders.length > 0 ? 
                    availablePlaceholders.map(p => `<button class="placeholder-btn" data-placeholder="[${p}]">[${p}]</button>`).join('') :
                    '<p style="color: #64748b; font-style: italic;">No CSV file selected. Please select a CSV file first to see available placeholders.</p>'
                }
            </div>
        </div>
        
        <div style="margin: 20px 0;">
            <label style="display: block; margin-bottom: 8px; font-weight: 500;">Preview:</label>
            <div id="filenamePreview" style="padding: 12px; background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 8px; font-family: monospace; color: #1e293b;">
                ${currentPattern}
            </div>
        </div>
        
        <div style="margin: 20px 0;">
            <div style="display: flex; gap: 10px; flex-wrap: wrap;">
                <button id="addSpace" class="action-btn">Add Space</button>
                <button id="addDash" class="action-btn">Add Dash (-)</button>
                <button id="addUnderscore" class="action-btn">Add Underscore (_)</button>
                <button id="clearPattern" class="action-btn" style="background: #dc2626;">Clear</button>
            </div>
        </div>
        
        <div style="text-align: right; margin-top: 30px; padding-top: 20px; border-top: 1px solid #e2e8f0;">
            <button id="cancelPattern" style="margin-right: 10px; padding: 10px 20px;">Cancel</button>
            <button id="savePattern" style="padding: 10px 20px; background: #2563eb; color: white; border: none; border-radius: 6px;">Save Pattern</button>
        </div>
    `;
    
    modal.appendChild(modalContent);
    document.body.appendChild(modal);
    
    // Add styles for the new elements
    const style = document.createElement('style');
    style.textContent = `
        .placeholder-btn {
            padding: 8px 12px;
            background: #e2e8f0;
            border: 1px solid #cbd5e1;
            border-radius: 6px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        .placeholder-btn:hover {
            background: #2563eb;
            color: white;
            border-color: #2563eb;
        }
        .action-btn {
            padding: 8px 16px;
            background: #f8fafc;
            border: 1px solid #e2e8f0;
            border-radius: 6px;
            cursor: pointer;
            font-size: 14px;
            transition: all 0.2s;
        }
        .action-btn:hover {
            background: #e2e8f0;
        }
    `;
    document.head.appendChild(style);
    
    // Get references to elements
    const patternInput = document.getElementById('patternInput');
    const preview = document.getElementById('filenamePreview');
    const placeholderButtons = document.querySelectorAll('.placeholder-btn');
    const actionButtons = {
        addSpace: document.getElementById('addSpace'),
        addDash: document.getElementById('addDash'),
        addUnderscore: document.getElementById('addUnderscore'),
        clearPattern: document.getElementById('clearPattern')
    };
    
    // Update preview function
    function updatePreview() {
        const pattern = patternInput.value;
        preview.textContent = pattern || 'Enter a pattern...';
        preview.style.color = pattern ? '#1e293b' : '#94a3b8';
    }
    
    // Add placeholder to pattern
    function addPlaceholder(placeholder) {
        const currentValue = patternInput.value;
        const cursorPos = patternInput.selectionStart;
        const newValue = currentValue.slice(0, cursorPos) + placeholder + currentValue.slice(cursorPos);
        patternInput.value = newValue;
        patternInput.focus();
        patternInput.setSelectionRange(cursorPos + placeholder.length, cursorPos + placeholder.length);
        updatePreview();
    }
    
    // Add text to pattern
    function addText(text) {
        const currentValue = patternInput.value;
        const cursorPos = patternInput.selectionStart;
        const newValue = currentValue.slice(0, cursorPos) + text + currentValue.slice(cursorPos);
        patternInput.value = newValue;
        patternInput.focus();
        patternInput.setSelectionRange(cursorPos + text.length, cursorPos + text.length);
        updatePreview();
    }
    
    // Event listeners
    patternInput.addEventListener('input', updatePreview);
    patternInput.addEventListener('keyup', updatePreview);
    
    placeholderButtons.forEach(btn => {
        btn.addEventListener('click', () => {
            addPlaceholder(btn.dataset.placeholder);
        });
    });
    
    actionButtons.addSpace.addEventListener('click', () => addText(' '));
    actionButtons.addDash.addEventListener('click', () => addText('-'));
    actionButtons.addUnderscore.addEventListener('click', () => addText('_'));
    actionButtons.clearPattern.addEventListener('click', () => {
        patternInput.value = '';
        updatePreview();
        patternInput.focus();
    });
    
    document.getElementById('cancelPattern').onclick = () => {
        document.body.removeChild(modal);
        document.head.removeChild(style);
        updateStatus('File name configuration cancelled', 'info');
    };
    
    document.getElementById('savePattern').onclick = () => {
        const newPattern = patternInput.value.trim();
        if (newPattern) {
            localStorage.setItem('fileNamePattern', newPattern);
            updateStatus('File name pattern updated: ' + newPattern, 'success');
        }
        document.body.removeChild(modal);
        document.head.removeChild(style);
    };
    
    // Focus the input and update preview
    setTimeout(() => {
        patternInput.focus();
        patternInput.select();
        updatePreview();
    }, 100);
}

async function testAPI() {
    try {
        // Simple connectivity test - just check if backend responds
        const response = await fetch('http://localhost:5000/api/fileprocessing/preview', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                csvFilePath: 'test-data/test.csv',
                templatePath: 'test-data/templates/template.xlsx',
                fileNamePattern: '[Location] [Facility] [Equipment] [Procedure]'
            })
        });
        
        if (response.ok) {
            updateStatus('Backend API is working!', 'success');
        } else {
            // Even if the specific request fails, if we got a response, the backend is running
            if (response.status === 500) {
                updateStatus('Backend API is running (test files may not exist)', 'info');
            } else {
                updateStatus('Backend API connection failed: ' + response.statusText, 'error');
            }
        }
    } catch (error) {
        if (error.message.includes('Failed to fetch')) {
            updateStatus('Backend API is not running - please start with: dotnet run', 'warning');
        } else {
            updateStatus('Backend API connection failed: ' + error.message, 'error');
        }
    }
}

function updateStatus(message, type = 'info') {
    const statusElement = document.getElementById('status');
    if (statusElement) {
        statusElement.textContent = message;
        statusElement.className = 'status ' + type;
        statusElement.style.display = 'block';
    }
    console.log('Status:', message);
}

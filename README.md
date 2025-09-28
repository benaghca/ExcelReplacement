# Excel Replacement - Document Template Processor

A cross-platform desktop application built with Electron and .NET that processes Excel and Word templates by replacing placeholders with data from a CSV file.

## Architecture

This application consists of two main parts:
- **Frontend**: Electron-based desktop app with TypeScript/JavaScript
- **Backend**: .NET 8 Web API that handles file processing

## What it does

This tool allows you to:
- Select a CSV file containing your data
- Select an Excel or Word template containing placeholders
- Preview and validate your template before processing
- Choose an output directory
- Generate individual files for each row in your CSV

## How to use

### Development Setup

1. **Install dependencies**:
   ```bash
   npm install
   ```

2. **Build the backend**:
   ```bash
   dotnet build
   ```

3. **Start the backend** (in one terminal):
   ```bash
   dotnet run
   ```

4. **Start the frontend** (in another terminal):
   ```bash
   npm run dev
   ```

### Using the application

1. **Select the template type** (Excel or Word)
2. **Select your CSV file** with replacement data
3. **Select your template file** with placeholders
4. **Click "Preview Template"** to validate your setup
5. **Choose an output directory** for the generated files
6. **Click "Process Files"** to start
7. The application will process the files and display progress
8. When complete, you'll see a summary of the processed records

## Template Preview and Validation

The preview feature helps you catch issues before processing large batches:

1. **Validation Tab**:
   - Shows all placeholders found in your template
   - Indicates if each placeholder has a matching CSV column
   - Displays sample values from your CSV
   - Shows the location of each placeholder in your document

2. **Preview Tab**:
   - Shows how your template will look with the first row of data
   - Displays content from all sheets (Excel) or sections (Word)
   - Helps verify formatting and layout

3. **Status Information**:
   - Shows the number of errors and warnings
   - Highlights unused CSV columns
   - Indicates missing placeholders

## Building for Distribution

### Backend Distribution

1. **Publish the .NET backend**:
   ```bash
   dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
   ```

2. **Locate the backend executable**:
   - The executable will be in the `bin\Release\net8.0\win-x64\publish` directory
   - The file will be named `ExcelReplacement.exe`

### Frontend Distribution

1. **Build the Electron app**:
   ```bash
   npm run build
   ```

2. **Locate the built app**:
   - The built app will be in the `dist` directory
   - For Windows: `dist/win-unpacked/excel-replacement.exe`

### Complete Distribution

The application requires both the backend and frontend to be running:
- Start the backend: `dotnet run` or run the published executable
- Start the frontend: `npm run dev` or run the built Electron app

## File requirements

### CSV File
- Must be properly formatted with headers in the first row
- Must contain the following required fields:
  - Location
  - Facility
  - Equipment
  - Procedure
  - Lineup
- Can contain additional fields as needed

Example CSV format:
```
Location,Facility,Equipment,Procedure,Lineup,Panel-1,Breaker-1,Breaker-2
Location1,FacilityA,Equipment-01,Maintenance,Line1,MP-11,CKT-34/36/38,CKT-28/30/32
```

### Template Files
- Must be a valid .xlsx (Excel) or .docx (Word) file
- Use placeholders in the format `[FieldName]` where FieldName matches a CSV header (case-sensitive, no extra spaces)
- Example placeholders: `[Location]`, `[Facility]`, `[Equipment]`, etc.
- You can combine placeholders: `[Location] [Facility]`
- Placeholders can be used anywhere in your document, including inside tables, headers, and footers
- **Formatting is preserved:** You can use bold, italics, and other formatting in your templates. Only the replaced text will change; the rest of your formatting will remain intact.

## How it works

1. The application reads the CSV file and extracts data for each row
2. For each row, it:
   - Creates a copy of the template
   - Finds text containing placeholders
   - Replaces placeholders with the corresponding data from the CSV
   - Saves the file with a name generated from the CSV data
   - Moves to the next row

## Troubleshooting

### Common issues

1. **File not found errors**:
   - Ensure your CSV and template files exist and are accessible

2. **Format errors**:
   - Check that your CSV is properly formatted with headers
   - Ensure your template is a valid .xlsx or .docx file

3. **Missing fields**:
   - Make sure your CSV contains all required fields
   - Check for spelling mismatches between CSV headers and placeholders

4. **Access denied errors**:
   - Ensure you have write permissions for the output directory

5. **Placeholders not being replaced or formatting issues:**
   - Make sure placeholders use the format `[FieldName]` with single square brackets
   - Placeholders must match CSV headers exactly (case-sensitive, no extra spaces)
   - Placeholders can be inside tables, headers, footers, or split across formatting (bold, italics, etc.)
   - If you see formatting issues, ensure your template is using standard Word formatting and not special objects

## Sample files

The repository includes:
- `test_data.csv`: Sample CSV file with test data
- `template.xlsx`: Sample Excel template with placeholders

You can use these to test the application.

## Version 3.0.0 - Major UI/UX Overhaul

### 🎨 New Professional Interface
- **Clean, modern design** - Replaced Matrix theme with professional blue/gray color scheme
- **Improved typography** - System fonts for better readability
- **Enhanced visual hierarchy** - Better spacing, shadows, and layout

### 📁 Advanced File Name Configuration
- **Interactive placeholder builder** - Click to add CSV column placeholders
- **Real-time preview** - See filename pattern as you build it
- **Smart suggestions** - Available placeholders displayed as clickable buttons
- **Action buttons** - Quick add spaces, dashes, and underscores
- **Persistent settings** - Your patterns are saved and remembered

### 🗂️ Professional Template Manager
- **Database-style table view** - Scalable interface for managing many templates
- **Template preview panel** - See detailed info before loading
- **Template organization** - Name, type, category, and date tracking
- **Bulk operations** - Load, delete, and manage multiple templates
- **Scrollable interface** - Handles 100+ templates efficiently

### 🔧 Enhanced User Experience
- **Drag & drop support** - Drop files directly into the application
- **Better error handling** - Clear, actionable error messages
- **Improved file dialogs** - Native file selection with proper filtering
- **Status feedback** - Real-time progress and status updates
- **Modal dialogs** - Professional popup interfaces for configuration

### 🚀 Performance Improvements
- **Faster file processing** - Optimized template processing
- **Better memory management** - Efficient handling of large files
- **Improved stability** - Reduced crashes and better error recovery
- **Enhanced file validation** - Better template and CSV validation

### 🛠️ Technical Improvements
- **Modernized codebase** - Cleaner, more maintainable code
- **Better separation of concerns** - Improved architecture
- **Enhanced testing** - Comprehensive test coverage
- **Improved documentation** - Better README and code comments
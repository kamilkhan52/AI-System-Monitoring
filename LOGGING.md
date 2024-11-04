# Logging System Documentation

## Overview
The application uses a centralized logging system with different verbosity levels and file rotation. Logs are stored in `debug.log` in the application directory.

## Log Levels
From most to least verbose:
1. **Trace**: Detailed debugging information
   - Timer ticks
   - Process execution details
   - Line-by-line parsing
   
2. **Info**: Normal application operation
   - Application startup
   - Configuration changes
   - Processor info updates
   - Cache metrics updates

3. **Warning**: Potential issues that don't affect core functionality
   - Missing files
   - Failed data collection attempts
   - Configuration issues

4. **Error**: Serious issues that affect functionality
   - Process execution failures
   - File access errors
   - Parsing errors

## Configuration
Logging can be configured through the Settings window:
- **Enable Debug Logging**: Toggle all logging on/off
- **Log Level**: Set minimum level to log
  - Setting to "Info" will hide Trace messages
  - Setting to "Warning" will hide both Trace and Info messages

## Log File Management
- Location: `[Application Directory]/debug.log`
- Auto-rotation: Files over 10MB are automatically archived to `debug.log.old`
- Format: `[Timestamp]|[Level]|[Message]`
  ```
  2024-10-29 11:11:06|Info|SystemMetricsViewModel initialized
  2024-10-29 11:11:06|Trace|Timer ticked
  2024-10-29 11:11:06|Error|Failed to parse metrics data
  ```

## For Developers
The logging system is implemented in `LoggingService.cs` as a singleton: 

## Troubleshooting
1. If logs aren't appearing:
   - Check if logging is enabled in Settings
   - Verify application has write permissions
   - Check log level setting

2. For performance issues:
   - Set log level to Warning or Error
   - Enable logging only when debugging

3. For disk space concerns:
   - Logs auto-rotate at 10MB
   - Manually clear `debug.log` and `debug.log.old`
# AI System Monitoring

[![Version](https://img.shields.io/github/v/release/kamilkhan52/AI-System-Monitoring)](https://github.com/kamilkhan52/AI-System-Monitoring/releases)
[![License](https://img.shields.io/github/license/kamilkhan52/AI-System-Monitoring)](LICENSE)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com/download/dotnet/8.0)

Real-time System Performance Monitor with AI-Powered Analysis

## Table of Contents
- [Overview](#overview)
- [Key Features](#key-features)
- [Screenshots](#screenshots)
- [Requirements](#requirements)
- [Installation](#installation)
- [Configuration](#configuration)
- [Building from Source](#building-from-source)
- [Contributing](#contributing)
- [License](#license)
- [Acknowledgments](#acknowledgments)
- [Environment Setup](#environment-setup)
- [Known Issues](#known-issues)

## Overview

AI System Monitoring is a lightweight, customizable Windows desktop application that provides real-time monitoring of system performance metrics with AI-powered analysis. Built with WPF (.NET 8.0), it offers an intuitive interface for tracking CPU, memory, and disk usage, along with advanced processor metrics and intelligent system recommendations.

## Screenshots

![AI System Monitor showing real-time performance analysis with AI insights](screenshot.png)

## Key Features

* Real-time monitoring:
  * CPU usage (total and per-core)
  * Memory usage tracking
  * Disk usage statistics
  * Advanced processor metrics (L1/L2 cache, CCD info, memory channels)
* AI-powered system analysis and recommendations
* Customizable features:
  * Polling intervals
  * Click-through and always-on-top modes
  * Configurable metric visibility
* Dark mode interface

## Requirements

* Windows OS
* .NET 8.0 Runtime
* AMD uProf (for cache-related metrics)
* Perplexity API key (for AI analysis features)

## Installation

1. Download the latest release from the [Releases](https://github.com/kamilkhan52/AI-System-Monitoring/releases) page
2. Run the executable - no installation required
3. The application will prompt for your Perplexity API key on first launch
4. **Important**: Run as administrator for AMD uProf metrics to work correctly

## Configuration

Access settings through the gear icon to customize:

```json
{
  "updateInterval": 5,  // seconds (1-10)
  "visibleMetrics": {
    "cpu": true,
    "memory": true,
    "disk": true,
    "advanced": false
  },
  "windowBehavior": {
    "alwaysOnTop": false,
    "clickThrough": false
  }
}
```

Logging options:
- Enable/disable logging
- Set log level (Debug, Info, Warning, Error)
- Logs are stored in the application directory

## Building from Source

```bash
# Clone the repository
git clone https://github.com/kamilkhan52/AI-System-Monitoring.git

# Navigate to project directory
cd AI-System-Monitoring

# Restore dependencies
dotnet restore

# Build the solution
dotnet build
```

## Contributing

We welcome contributions! Please feel free to submit a Pull Request.

### Development Prerequisites

* Visual Studio 2022 or later
* .NET 8.0 SDK
* Git

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

* [Perplexity](https://perplexity.ai/) for AI capabilities
* [AMD uProf](https://developer.amd.com/uprof/) for processor metrics
* WPF community

## Environment Setup

Before running the application, set up the following environment variable:
```bash
PERPLEXITY_API_KEY=your_api_key_here
```

Alternatively, you can enter the API key when prompted on first launch.

## Known Issues

- Cache metrics require AMD processors with uProf support
- Some features require administrator privileges
- Windows-only support currently
# WinLauncher - 现代化的 Windows 应用启动器 🚀

<div align="center">

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple)
![Platform](https://img.shields.io/badge/platform-Windows-0078d7)
![Architecture](https://img.shields.io/badge/architecture-MVVM-green)

**让 Windows 应用启动变得优雅而高效**

[特性](#-特性) • [安装](#-安装) • [开发](#-开发) • [贡献](#-贡献)

</div>

## ✨ 项目简介

WinLauncher 是一款专为 Windows 设计的现代化应用启动器，灵感来源于 macOS 的 Launchpad。它提供了优雅的界面和强大的功能，让您能够快速找到并启动任何应用程序。

### 🎯 为什么选择 WinLauncher？

| 特性 | 描述 |
|------|------|
| 💫 **现代化设计** | 采用 Fluent Design 设计语言，毛玻璃效果和流畅动画 |
| ⚡ **极速搜索** | 智能搜索算法，毫秒级响应，支持模糊匹配 |
| 🔍 **全面扫描** | 自动发现所有已安装应用，包括桌面程序、UWP 应用和商店应用 |
| 🎨 **高度可定制** | 支持图标大小、主题、动画等个性化设置 |
| 🛡️ **安全可靠** | 本地数据存储，保护您的隐私 |

## 🚀 核心特性

### 智能应用发现
- **多策略扫描**：采用桌面扫描、UWP 扫描、商店应用扫描等多种策略
- **自动去重**：智能识别并合并重复的应用
- **图标提取**：高质量图标提取，支持多种格式和尺寸
- **并行处理**：多线程扫描，大幅提升性能

### 卓越的用户体验
- **流畅动画**：精心设计的交互动效，提升使用感受
- **实时搜索**：输入即搜，支持拼音和模糊匹配
- **骨架屏加载**：优雅的加载状态提示
- **响应式布局**：自适应不同屏幕尺寸

### 技术亮点
- **现代化架构**：基于 MVVM 模式，清晰的代码结构
- **依赖注入**：使用 Microsoft.Extensions.DependencyInjection
- **异常处理**：完善的错误处理和日志记录系统
- **性能监控**：内置性能追踪和内存管理

## 🛠️ 技术栈

### 前端技术
- **WPF** - 现代化桌面应用框架
- **XAML** - 声明式 UI 设计
- **Fluent Design** - 现代化设计语言

### 后端技术
- **.NET 8.0** - 最新的 .NET 运行时
- **C# 10+** - 现代化 C# 特性
- **Windows API** - 原生 Windows 集成

### 开发工具
- **CommunityToolkit.Mvvm** - MVVM 工具包
- **Newtonsoft.Json** - JSON 序列化
- **System.Drawing** - 图像处理

## 📦 安装与使用

### 系统要求
- **操作系统**: Windows 10/11 (x64)
- **运行时**: .NET 8.0 Runtime
- **权限**: 管理员权限（用于完整应用扫描）

### 快速开始

1. **下载最新版本**
   ```bash
   # 从 Releases 页面下载安装包
   # 或克隆源码自行编译
   git clone https://github.com/your-username/WinLauncher.git
   ```
2. **运行应用**
   ```bash
   cd WinLauncher
   dotnet run
   # 或以管理员身份运行 WinLauncher.exe 获取完整功能
   ```
3. **开始使用**
  - 应用会自动扫描并显示所有程序
  - 使用搜索框快速查找应用
  - 点击应用图标即可启动


### 构建说明
   ```bash
   # 克隆项目
   git clone https://github.com/your-username/WinLauncher.git
   # 还原依赖
   dotnet restore
   # 构建项目
   dotnet build --configuration Release
   # 发布应用
   dotnet publish --configuration Release --self-contained
   ```


##  🏗️ 项目架构

### 核心架构
   ```text
   WinLauncher/
   ├── Core/                 # 核心业务逻辑
   │   ├── Interfaces/      # 服务接口定义
   │   ├── Models/         # 数据模型
   │   └── Enums/          # 枚举类型
   ├── Infrastructure/      # 基础设施层
   │   ├── Helpers/        # 工具类
   │   ├── Services/       # 服务实现
   │   └── Strategies/     # 扫描策略
   ├── ViewModels/         # 视图模型 (MVVM)
   ├── Views/              # 用户界面
   └── Styles/             # 样式和资源
   ```

### 设计模式
   - MVVM模式: 清晰的关注点分离
   - 依赖注入: 松耦合的组件设计
   - 策略模式: 可扩展的扫描策略
   - 观察者模式: 响应式数据绑定

### 🤝 贡献指南
欢迎所有形式的贡献！请阅读我们的贡献指南：

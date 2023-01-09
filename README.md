

[中文](README.md) [English](README_EN.md) 


<h1 align="center">SuperCom</h1>

[![.NET CORE](https://img.shields.io/badge/.NET%20Framework-4.7.2-d.svg)](#)
[![Platform](https://img.shields.io/badge/Platform-Win-brightgreen.svg)](#)
[![LICENSE](https://img.shields.io/badge/license-GPL%203.0-blue)](#)
[![Star](https://img.shields.io/github/stars/SuperStudio/SuperCom?label=Star%20this%20repo)](https://github.com/SuperStudio/SuperCom)
[![Fork](https://img.shields.io/github/forks/SuperStudio/SuperCom?label=Fork%20this%20repo)](https://github.com/SuperStudio/SuperCom/fork)

SuperCom 是一款**串口调试工具**，用于 Window 串口的调试

下载地址：[点此下载](https://github.com/SuperStudio/SuperCom/releases)

<img src="Image/image-20220828232341836.png" alt="image-20220828232341836" style="zoom:80%;" />

# 关于

SuperCom 是一款**美观**、**流畅**、**功能丰富**、**高效**的串口调试工具，支持以下特点：

- 同时打开多个串口进行监听
- 串口日志自动保存
- 串口日志支持滚屏/固定
- 可以设置波特率、位大小等串口设置
- 可发送各种 AT 指令
- 具有各种特性

## 创新特性

**一、语法高亮**

<img src="Image/image-20230109234347852.png" alt="image-20230109234347852" style="zoom:80%;" />

可自定义

<img src="Image/image-20230109234412138.png" alt="image-20230109234412138" style="zoom:80%;" />

**二、命令自动运行**

<img src="Image/1.gif" alt="1" style="zoom:80%;" />

或者

<img src="Image/2.gif" alt="2" style="zoom:80%;" />

**三、快捷键**

<img src="Image/image-20230109234634330.png" alt="image-20230109234634330" style="zoom:80%;" />

**四、插件与皮肤**

<img src="Image/3.gif" alt="3" style="zoom:80%;" />

切换皮肤

<img src="Image/4.gif" alt="4" style="zoom:80%;" />

五、在线升级

<img src="Image/5.gif" alt="5" style="zoom:80%;" />

六、支持 HEX 与字符串互转

<img src="Image/hex.gif" alt="hex" style="zoom:80%;" />

七、支持时间戳与北京时间互转

<img src="Image/time.gif" alt="time" style="zoom:80%;" />

八、格式化 JSON 与合并行

<img src="Image/6.gif" alt="6" style="zoom:80%;" />

# 文档

用户文档：[Wiki](https://github.com/SuperStudio/SuperCom/wiki)

开发者文档：

# 分支说明

| 分支名           | 说明                                                   |
| ---------------- | ------------------------------------------------------ |
| master           | 主分支，其它用户拉取的主要代码，同时也是 PR 的目标分支 |
| dev-chao         | 私人的开发分支，避免影响到他人拉取                     |
| release_20220930 | 发布分支，用于各个项目引用，保证稳定的依赖关系         |

每 3 个月更新一次 release 分支


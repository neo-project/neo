小蚁 AntShares
================

小蚁是基于区块链技术，将实体世界的资产和权益进行数字化，通过点对点网络进行登记发行、转让交易、清算交割等金融业务的去中心化网络协议。

Antshares is a decentralized and distributed network protocol which is based on blockchain technology. People can use it to digitalize assets or shares, and accomplish some financial business through peer-to-peer network such as registration and issuing, make transactions, settlement and payment.

支持平台
--------

|   | AntShares |
|---|:-----------:|
|**CentOS 7.1**|:white_check_mark:|
|**Docker**|:white_check_mark:|
|**Red Hat 7.1**|:white_check_mark:|
|**Ubuntu**|:white_check_mark:|
|**Windows 7 SP1**|:white_check_mark:|
|**Windows Server 2008 R2**|:white_check_mark:|
未来将支持Debian、FreeBSD、Linux Mint、openSUSE、Oracle Linux、OS X、Fedora等。

如何开发
--------

在 Windows 下，使用 [Visual Studio 2015](https://www.visualstudio.com/products/visual-studio-community-vs) 来开发和编译本项目是最方便的。

To install AntShares, run the following command in the [Package Manager Console](https://docs.nuget.org/ndocs/tools/package-manager-console):

```
PM> Install-Package AntShares
```

在 Linux 或 MAC OS 下，可以使用任何你喜欢的开发工具来开发，然后使用 [.Net Core SDK](https://www.microsoft.com/net/core) 来生成项目。

此外，你还可以通过调用 API 或者 SDK 的方式来开发基于小蚁的应用：

+ [JSON-RPC](https://github.com/AntShares/AntShares/wiki/API%E5%8F%82%E8%80%83)
+ [C# SDK](https://github.com/AntShares/AntShares/tree/master/AntSharesCore)
+ [JAVA SDK](https://github.com/AntSharesSDK/antshares-java)
+ [JavaScript/TypeScript SDK](https://github.com/AntSharesSDK/antshares-ts)

这里还有几个可以参考的应用案例：

+ [AntSharesCore](https://github.com/AntShares/AntSharesCore) 包含命令行及图形界面的小蚁客户端
+ [AntSharesApp](https://github.com/AntShares/AntSharesApp) 基于Cordova开发的跨平台小蚁APP
+ [AntChain.xyz](https://github.com/lcux/antchain.xyz) 用于查看小蚁区块链内容的浏览器

项目介绍
--------

+ 白皮书：[中文](https://github.com/AntShares/AntShares/wiki/%E7%99%BD%E7%9A%AE%E4%B9%A6-1.1)|[English](https://github.com/AntShares/AntShares/wiki/Whitepaper-1.1)
+ 共识机制：[dBFT](http://www.onchain.com/paper/66c6773b.pdf)
+ 虚拟机：[AntShares.VM](https://github.com/AntShares/AntShares.VM)
+ 部署文档：[记账节点](https://github.com/AntShares/AntShares/wiki/%E8%AE%B0%E8%B4%A6%E8%8A%82%E7%82%B9)

如何贡献
--------

小蚁项目非常欢迎贡献者。

最简单的贡献方式，就是参与讨论，并向我们提出产品的改进意见。此外，我们也非常欢迎测试和提交BUG。

如果你希望提交代码，请通过 Pull Request 的方式提交，并确保你的代码符合以下要求：

1. 具有详细的功能说明
1. 具有完整的单元测试
1. 具有充分的注释

我们会在收到请求后，及时处理提交。

许可证
------

小蚁项目基于 MIT 协议开发和发布，详细条款请见 [LICENSE](https://github.com/AntShares/AntShares/blob/master/LICENSE) 文件。

联系我们
------------

[![Join the chat at https://gitter.im/AntShares/Lobby](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AntShares/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

官网：https://www.antshares.org/

论坛：http://8btc.com/antshares

QQ群：23917224

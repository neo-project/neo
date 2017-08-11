NEO
================

NEO，原名小蚁，2014年正式立项，2015年6月于Github实时开源。成立以来，NEO团队亲历了区块链行业的高潮与低谷，数字货币市场的狂热与冷却，各国监管与政府态度的模糊与清晰。我们相信，科技是这个时代变迁的原动力，在这股动力的推动下，我们将迈入新的“智能经济”时代。

NEO区块链通过将点对点网络、拜占庭容错、数字证书、智能合约、超导交易、跨链互操作协议等一系列技术相结合，让你快速、高效、安全、合法地管理你的智能资产。

NEO, formerly called Antshares, is China's first ever open source blockchain. Founded in 2014, NEO’s mission has been to reinvent the way commerce is done. We believe technology drives progress and that together, we can create the future. Motivated by this, NEO has been created to shift our traditional economy into the new era of the Smart Economy. 

Through technologies such as P2P networking, dBFT consensus, digital certificates, Superconducting Transactions, and cross-chain interoperability, the NEO blockchain enables management of smart assets in an efficient, safe and legally binding manner.

| 支持平台 - Supported Platforms  | NEO Blockchain |
|---|:-----------:|
|**CentOS 7.1**|:white_check_mark:|
|**Docker**|:white_check_mark:|
|**Red Hat 7.1**|:white_check_mark:|
|**Ubuntu**|:white_check_mark:|
|**Windows 7 SP1**|:white_check_mark:|
|**Windows Server 2008 R2**|:white_check_mark:|

未来将支持Debian、FreeBSD、Linux Mint、openSUSE、Oracle Linux、OS X、Fedora等。

In the future, we will support Debian, FreeBSD, Linux Mint, openSUSE, Oracle Linux, OS X, Fedora and so on.

如何开发 | How to Develop
--------

在 Windows 下，使用 [Visual Studio 2015](https://www.visualstudio.com/products/visual-studio-community-vs) 来开发和编译本项目是最方便的。

Under Windows, it is most convenient to develop and compile this project using [Visual Studio 2015](https://www.visualstudio.com/products/visual-studio-community-vs).

To install NEO, run the following command in the [Package Manager Console](https://docs.nuget.org/ndocs/tools/package-manager-console):

```
PM> Install-Package AntShares 
```
<!--the instructions need to be updated-->
在 Linux 或 MAC OS 下，可以使用任何你喜欢的开发工具来开发，然后使用 [.Net Core SDK](https://www.microsoft.com/net/core) 来生成项目。此外，你还可以通过调用 API 或者 SDK 的方式来开发基于NEO应用.

Under Linux or Mac OS, you can use any of your favorite development tools to develop, and then use the .Net Core SDK to generate projects. In addition, you can also use the API or SDK way to develop applications based on NEO：
<!--this list of applications should be updated to NEO-->
+ [JSON-RPC](https://github.com/AntShares/AntShares/wiki/API%E5%8F%82%E8%80%83)
+ [C# SDK](https://github.com/AntShares/AntShares/tree/master/AntSharesCore)
+ [JAVA SDK](https://github.com/AntSharesSDK/antshares-java)
+ [JavaScript/TypeScript SDK](https://github.com/AntSharesSDK/antshares-ts)

这里还有几个可以参考的应用案例 / Here are a few examples of applications that can be referenced:

+ [AntSharesCore](https://github.com/AntShares/AntSharesCore) 包含命令行及图形界面的小蚁客户端
+ [AntSharesApp](https://github.com/AntShares/AntSharesApp) 基于Cordova开发的跨平台小蚁APP
+ [AntChain.xyz](https://github.com/lcux/antchain.xyz) 用于查看小蚁区块链内容的浏览器

项目介绍 | Project Introduction
--------

<!--These should be updated-->
+ 白皮书：[中文](https://newneolink)|[English](https://newneolink)
+ 共识机制：[dBFT](http://www.onchain.com/paper/66c6773b.pdf)
+ 虚拟机：[NeoVM](https://github.com/neo-project/neo-vm) <!--is this the correct one?-->
+ 部署文档：[记账节点](https://github.com/AntShares/AntShares/wiki/%E8%AE%B0%E8%B4%A6%E8%8A%82%E7%82%B9)

+ Official Whitepaper: [Chinese](https://newneolink) | [English](https://newneolink) <!--these also-->
+ Consensus mechanism: Byzantine Fault Tolerance or [dBFT](http://www.onchain.com/paper/66c6773b.pdf)
+ Virtual Machine: [NeoVM](https://github.com/neo-project/neo-vm) <!--is this the correct one?-->
+ Placeholder: [Placeholder](https://something) <!--not sure what this links to, old github directory does not exist)

如何贡献
--------

小蚁项目非常欢迎贡献者。

最简单的贡献方式，就是参与讨论，并向我们提出产品的改进意见。此外，我们也非常欢迎测试和提交BUG。

如果你希望提交代码，请通过 Pull Request 的方式提交，并确保你的代码符合以下要求：

1. 具有详细的功能说明
1. 具有完整的单元测试
1. 具有充分的注释

我们会在收到请求后，及时处理提交。

How to Contribute
--------

Every project is welcome, no matter how small.

The easiest way to contribute is to participate in the discussion and to advise us on product improvements. In addition, you are also very welcome to test and submit bugs.

If you wish to submit your code, please submit it via Pull Request and make sure your code meets the following requirements:

1. With detailed functional description
1. Have a complete unit test
1. Have full notes

We will process the submission promptly upon receipt of the request.

许可证 | License
------

小蚁项目基于 MIT 协议开发和发布，详细条款请见 [LICENSE](https://github.com/AntShares/AntShares/blob/master/LICENSE) 文件。

The project is developed and published based on the MIT protocol. See the [license](https://github.com/AntShares/AntShares/blob/master/LICENSE) file for detailed terms. <!--this should be a NEO license-->

联系我们 | Contact Us
------------

[![Join the chat at https://gitter.im/AntShares/Lobby](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AntShares/Lobby?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge) <!--this is not up to date -->

官网: https://neo.org/zh-cn#  
Official Website：https://neo.org/en-us/#

论坛：http://8btc.com/antshares <!--is there a new NEO page already? I cannot access it as a foreign guest-->

QQ群：23917224  
QQ-group: 23917224 <!--not sure if this can be used outside of China, if it can't then it is okay if it's only in Chinese-->

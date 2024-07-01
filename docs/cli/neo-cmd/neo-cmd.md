# URI Scheme

## Syntax

The URI generic syntax consists of five components organized hierarchically in order of decreasing significance from left to right:

```
URI = scheme ":" [socket ":"] ["//" authority] path ["#" fragment]
```

A component is undefined if it has an associated delimiter and the delimiter does not appear in the URI; the scheme and path components are always defined. A component is empty if it has no characters; the scheme component is always non-empty.

The authority component consists of subcomponents:

```
authority = host [":" port]
```

The socket component consists of subcomponents:
```
socket = tcp | udp
```

## Examples

MainNet:

```
neo://+:10333/#860833102

OR

neo:tcp://+:10333/#860833102
```

TestNet:

```
neo://+:20333/#894710606

OR

neo:tcp://+:20333/#894710606
```

### Breakdown

Let's break down the following ***URI***:

> ```
> neo://127.0.0.1:226/#860833102,#89471060
> └┬┘   └────┬──────┘│└───────┬──────────┘
> scheme authority  path   fragment
> ```

- ***`neo` is the scheme.***
  - _Application(s) will connect to the ***tcp*** socket by ***default***._
- ***`//127.0.0.1:226` is the authority. Acts as passthrough to hit the _SeedList_***
  - _***127.0.0.1:226*** is the host and port the socket will connect too._
- ***`/` is the path.***
  - _***/*** doesn't do anything at this time._
- ***`#860833102,#89471060` is the fragment.***
  - _***#860833102,#89471060*** are the networks the application wants to join._


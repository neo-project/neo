# Loot
This is a migration of loot nft contract.
This also solves the random issue in loot.

![7772](https://user-images.githubusercontent.com/10189511/132791851-8ef79f6a-cff3-448b-81f8-3db12d86b705.png)

## Claim a new token

```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "invokefunction",
    "params": [
        "0xb78af146bd7aa6870bc5e005bb1134d5d1bfd2dc",
        "claim",
        [
            {
                "type": "Integer",
                "value": "7772"
            }
        ],
        [
            {
                "account": "0x8af673c2769fed72659b17217365b5077c173d9c",
                "scopes": "CalledByEntry"
            }
        ]
    ]
}
```
`response`
```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "script": "AVseEcAfDAVjbGFpbQwU3NK/0dU0EbsF4MULh6Z6vUbxirdBYn1bUg==",
        "state": "HALT",
        "gasconsumed": "19857460",
        "exception": null,
        "stack": [
            {
                "type": "Any"
            }
        ],
        "tx": "AHhuDV00AC8BAAAAAKgDCQAAAAAAh3sEAAGcPRd8B7VlcyEXm2Vy7Z92wnP2igEAKAFbHhHAHwwFY2xhaW0MFNzSv9HVNBG7BeDFC4emer1G8Yq3QWJ9W1IBQgxAHJrQavDMCW7hWxb3OdnzDSB0/wUXwQeeb3sPf82uW3Y1kB5ODpjfqa671OVJpL/NtLes82NjYJ4LkAZ3Gzk3oygMIQKUFixhqSbk60uFf+WBp2lM7TvCcatKcpiNfG/QJQs5XUFW57Mn"
    }
}
```
copy the `tx` filed and send  the transaction with `sendrawtranaction` API

## Get the loot token by calling tokenURI

```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "invokefunction",
    "params": [
        "0xb78af146bd7aa6870bc5e005bb1134d5d1bfd2dc",
        "tokenURI",
        [
            {
                "type": "Integer",
                "value": "7772"
            }
        ]
    ]
}
```
`response`
```json
{
    "jsonrpc": "2.0",
    "id": 1,
    "result": {
        "script": "AVweEcAfDAh0b2tlblVSSQwU3NK/0dU0EbsF4MULh6Z6vUbxirdBYn1bUg==",
        "state": "HALT",
        "gasconsumed": "6995805",
        "exception": null,
        "stack": [
            {
                "type": "ByteString",
                "value": "PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHByZXNlcnZlQXNwZWN0UmF0aW89InhNaW5ZTWluIG1lZXQiIHZpZXdCb3g9IjAgMCAzNTAgMzUwIj48c3R5bGU+LmJhc2UgeyBmaWxsOiB3aGl0ZTsgZm9udC1mYW1pbHk6IHNlcmlmOyBmb250LXNpemU6IDE0cHg7IH08L3N0eWxlPjxyZWN0IHdpZHRoPSIxMDAlIiBoZWlnaHQ9IjEwMCUiIGZpbGw9ImJsYWNrIiAvPjx0ZXh0IHg9IjEwIiB5PSIyMCIgY2xhc3M9ImJhc2UiPiBOMyBTZWN1cmUgTG9vdCAjNzc3MiA8L3RleHQ+PHRleHQgeD0iMTAiIHk9IjQwIiBjbGFzcz0iYmFzZSI+IE1hdWwgPC90ZXh0Pjx0ZXh0IHg9IjEwIiB5PSI2MCIgY2xhc3M9ImJhc2UiPiBTdHVkZGVkIExlYXRoZXIgQXJtb3Igb2YgUmFnZSA8L3RleHQ+PHRleHQgeD0iMTAiIHk9IjgwIiBjbGFzcz0iYmFzZSI+IEdyZWF0IEhlbG0gPC90ZXh0Pjx0ZXh0IHg9IjEwIiB5PSIxMDAiIGNsYXNzPSJiYXNlIj4gTGluZW4gU2FzaCA8L3RleHQ+PHRleHQgeD0iMTAiIHk9IjEyMCIgY2xhc3M9ImJhc2UiPiBEZW1vbmhpZGUgQm9vdHMgPC90ZXh0Pjx0ZXh0IHg9IjEwIiB5PSIxNDAiIGNsYXNzPSJiYXNlIj4gSGFyZCBMZWF0aGVyIEdsb3ZlcyA8L3RleHQ+PHRleHQgeD0iMTAiIHk9IjE2MCIgY2xhc3M9ImJhc2UiPiBOZWNrbGFjZSA8L3RleHQ+PHRleHQgeD0iMTAiIHk9IjE4MCIgY2xhc3M9ImJhc2UiPiAiUmFnZSBQZWFrIiBCcm9uemUgUmluZyBvZiBSZWZsZWN0aW9uICsxPC90ZXh0Pjwvc3ZnPg=="
            }
        ],
        "tx": "AB8q7X1dv2oAAAAAAIQJCQAAAAAAi3sEAAGcPRd8B7VlcyEXm2Vy7Z92wnP2igEAKwFcHhHAHwwIdG9rZW5VUkkMFNzSv9HVNBG7BeDFC4emer1G8Yq3QWJ9W1IBQgxAsdoIJKmcQDp8gmLOiN96ElbIBjeEQ0AebCC5tkK6s7B36V6EhZXF1cTGpPB9OOGedvc6iZWlfEKXhKntCEN2gCgMIQKUFixhqSbk60uFf+WBp2lM7TvCcatKcpiNfG/QJQs5XUFW57Mn"
    }
}
```

Decode the `value` with base64:
```html
<svg xmlns="http://www.w3.org/2000/svg" preserveAspectRatio="xMinYMin meet" viewBox="0 0 350 350">
    <style>
        .base {
            fill: white;
            font-family: serif;
            font-size: 14px;
        }
    </style>
    <rect width="100%" height="100%" fill="black" />
    <text x="10" y="20" class="base"> N3 Secure Loot #7772 </text>
    <text x="10" y="40" class="base"> Maul </text>
    <text x="10" y="60" class="base"> Studded Leather Armor of Rage </text>
    <text x="10" y="80" class="base"> Great Helm </text>
    <text x="10" y="100" class="base"> Linen Sash </text>
    <text x="10" y="120" class="base"> Demonhide Boots </text>
    <text x="10" y="140" class="base"> Hard Leather Gloves </text>
    <text x="10" y="160" class="base"> Necklace </text>
    <text x="10" y="180" class="base"> "Rage Peak" Bronze Ring of Reflection +1</text>
</svg>
```





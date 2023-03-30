# Bindables
Bindable Data Container Library for C#

## Usage

```cs
var b1 = new Bindable<int>(0);
var b2 = new Bindable<int>(99);
// b1 == 0
// b2 == 99

b1.BindTo(b2);
// b1 == 99
// b2 == 99

b1.Value = 5;
// b1 == 5
// b2 == 5
```

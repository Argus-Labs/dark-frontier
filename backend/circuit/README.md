# Dark Frontier Circuits

Dark Frontier circuits written using gnark

Note: In the `/artifacts` directory there must exist an `importer.go` file with the following in it:

```go
package artifacts

import _ "embed"

//go:embed init-uuid-cs
var ConstraintSystem []byte

//go:embed init-uuid-pk
var ProvingKey []byte

//go:embed init-uuid-vk
var VerifyingKey []byte
```
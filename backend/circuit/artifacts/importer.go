package artifacts

import _ "embed"

//go:embed init-084d8871-4cdb-46ab-b541-298cde6f9236-cs
var InitConstraintSystem []byte

//go:embed init-084d8871-4cdb-46ab-b541-298cde6f9236-pk
var InitProvingKey []byte

//go:embed init-084d8871-4cdb-46ab-b541-298cde6f9236-vk
var InitVerifyingKey []byte

//go:embed move-084d8871-4cdb-46ab-b541-298cde6f9236-cs
var MoveConstraintSystem []byte

//go:embed move-084d8871-4cdb-46ab-b541-298cde6f9236-pk
var MoveProvingKey []byte

//go:embed move-084d8871-4cdb-46ab-b541-298cde6f9236-vk
var MoveVerifyingKey []byte

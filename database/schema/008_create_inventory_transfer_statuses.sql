
CREATE TABLE dbo.InventoryTransferStatuses(
	TransferStatusCode VARCHAR(20) NOT NULL,
	DisplayName NVARCHAR(100) NOT NULL,
	IsTerminal BIT NOT NULL,

	CONSTRAINT PK_InventoryTransferStatuses
		PRIMARY KEY CLUSTERED (TransferStatusCode)
);
GO
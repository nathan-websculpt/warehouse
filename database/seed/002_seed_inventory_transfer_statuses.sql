
INSERT INTO dbo.InventoryTransferStatuses (
	TransferStatusCode,
	DisplayName,
	IsTerminal
)
VALUES
	('DRAFT', N'Draft', 0),
	('REQUESTED', N'Requested', 0),
	('APPROVED', N'Approved', 0),
	('REJECTED', N'Rejected', 1),
	('DISPATCHED', N'Dispatched', 0),
	('RECEIVED', N'Received', 1),
	('CANCELLED', N'Cancelled', 1);
GO
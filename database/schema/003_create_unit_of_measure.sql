
/*
	Global reference data for a product's base stocking unit

	NOTE: Package-level conversions between each, case, and pallet will come later
*/

CREATE TABLE dbo.UnitOfMeasure (
	UnitOfMeasureId INT IDENTITY (1,1) NOT NULL,
	UnitOfMeasureCode NVARCHAR(3) NOT NULL, -- EA, CS, PLT, LB, KG, GAL
	UnitOfMeasureName NVARCHAR(20) NOT NULL,

	CONSTRAINT PK_UnitOfMeasure
		PRIMARY KEY CLUSTERED (UnitOfMeasureId),

	CONSTRAINT UQ_UnitOfMeasure_UnitOfMeasureCode
		UNIQUE NONCLUSTERED (UnitOfMeasureCode),

	CONSTRAINT UQ_UnitOfMeasure_UnitOfMeasureName
		UNIQUE NONCLUSTERED (UnitOfMeasureName)
);
GO
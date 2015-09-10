CREATE TABLE [dbo].[A0_AgencyConfig]
(
	[ID] INT NOT NULL PRIMARY KEY identity, 
    [StartQuantity] INT NULL DEFAULT 0, 
    [EndQuantity] INT NOT NULL, 
    [MoneyPercent] FLOAT NOT NULL DEFAULT 0, 
    [AgencyBusinessID] INT NOT NULL
)

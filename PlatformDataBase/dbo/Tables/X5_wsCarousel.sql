CREATE TABLE [dbo].[X5_wsCarousel] (
    [AutoCode]  INT            IDENTITY (1, 1) NOT NULL,
    [dAddTime]  DATETIME       NULL,
    [lSiteCode] INT            NOT NULL,
    [sName]     NVARCHAR (20)  NULL,
    [sData]     NVARCHAR (MAX) NULL,
    [sComment]  NVARCHAR (256) NULL,
    [bDel]      TINYINT        NULL
);


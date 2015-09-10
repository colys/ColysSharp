CREATE TABLE [dbo].[X5_wsStaticHtml] (
    [AutoCode]      INT            IDENTITY (1, 1) NOT NULL,
    [sUrl]          NVARCHAR (255) NOT NULL,
    [sContent]      NVARCHAR (MAX) NULL,
    [lTemplateCode] INT            NULL,
    [dAddDate]      DATETIME       NULL
);


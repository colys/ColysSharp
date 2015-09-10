CREATE TABLE [dbo].[x5_wsCommentZan] (
    [AutoCode]     INT          IDENTITY (1, 1) NOT NULL,
    [sPersonCode]  VARCHAR (50) NOT NULL,
    [lCommentCode] INT          NOT NULL,
    [dCreateTime]  DATETIME     CONSTRAINT [DF_x5_wsComentZan_dCreateTime] DEFAULT (getdate()) NOT NULL
);


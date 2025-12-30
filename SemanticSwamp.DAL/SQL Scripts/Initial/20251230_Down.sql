USE SemanticSwamp
GO

/****** Object:  Table [dbo].[DocumentUploads]    Script Date: 12/18/2025 11:14:26 AM ******/
IF  EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[DocumentUploads]') AND type in (N'U'))
DROP TABLE [dbo].[DocumentUploads]
GO



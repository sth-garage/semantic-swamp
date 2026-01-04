USE SemanticSwamp
GO

/****** Object:  Table [dbo].[Terms]    Script Date: 1/3/2026 9:37:36 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Terms](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Terms] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO




/****** Object:  Table [dbo].[Categories]    Script Date: 1/3/2026 3:06:17 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Categories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


/****** Object:  Table [dbo].[Collections]    Script Date: 1/3/2026 3:05:46 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[Collections](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](2000) NOT NULL,
 CONSTRAINT [PK_Collections] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO


/****** Object:  Table [dbo].[DocumentUploadTerms]    Script Date: 1/4/2026 1:12:28 AM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DocumentUploadTerms](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TermId] [int] NOT NULL,
	[DocumentUploadId] [int] NOT NULL,
 CONSTRAINT [PK_DocumentUploadTerms] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DocumentUploadTerms]  WITH CHECK ADD  CONSTRAINT [FK_DocumentUploadTerms_DocumentUploads] FOREIGN KEY([DocumentUploadId])
REFERENCES [dbo].[DocumentUploads] ([Id])
GO

ALTER TABLE [dbo].[DocumentUploadTerms] CHECK CONSTRAINT [FK_DocumentUploadTerms_DocumentUploads]
GO

ALTER TABLE [dbo].[DocumentUploadTerms]  WITH CHECK ADD  CONSTRAINT [FK_DocumentUploadTerms_Terms] FOREIGN KEY([TermId])
REFERENCES [dbo].[Terms] ([Id])
GO

ALTER TABLE [dbo].[DocumentUploadTerms] CHECK CONSTRAINT [FK_DocumentUploadTerms_Terms]
GO




USE [SemanticSwamp]
GO

/****** Object:  Table [dbo].[DocumentUploadTerms]    Script Date: 1/3/2026 3:05:06 PM ******/
SET ANSI_NULLS ON
GO

SET QUOTED_IDENTIFIER ON
GO

CREATE TABLE [dbo].[DocumentUploadTerms](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[TermId] [int] NOT NULL,
	[DocumentUploadId] [int] NOT NULL,
 CONSTRAINT [PK_DocumentUploadTerms] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON, OPTIMIZE_FOR_SEQUENTIAL_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[DocumentUploadTerms]  WITH CHECK ADD  CONSTRAINT [FK_DocumentUploadTerms_Terms] FOREIGN KEY([TermId])
REFERENCES [dbo].[Terms] ([Id])
GO

ALTER TABLE [dbo].[DocumentUploadTerms] CHECK CONSTRAINT [FK_DocumentUploadTerms_Terms]
GO






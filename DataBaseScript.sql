CREATE TABLE dbo.Client
(
	Id            INT IDENTITY
		CONSTRAINT Client_pk
			PRIMARY KEY,
	Name          VARCHAR(256) NOT NULL,
	CompanyName   VARCHAR(256),
	StreetAddress VARCHAR(256),
	CityStateZip  VARCHAR(MAX),
	Phone         NVARCHAR(256)
)
go

CREATE UNIQUE INDEX Client_Phone_uindex
	ON dbo.Client ( Phone )
go

CREATE TABLE dbo.InvoiceHeader
(
	Id       INT IDENTITY
		CONSTRAINT InvoiceHeader_pk
			PRIMARY KEY,
	Date     DATETIME2      DEFAULT GETDATE( ) NOT NULL,
	Status   TINYINT        DEFAULT 0          NOT NULL,
	ClientId INT                               NOT NULL
		CONSTRAINT InvoiceHeader_Client_Id_fk
			REFERENCES dbo.Client
			ON DELETE CASCADE,
	DueDate  DATETIME2      DEFAULT GETDATE( ) NOT NULL,
	IsPaid   BIT            DEFAULT 0          NOT NULL,
	Subtotal DECIMAL(18, 6) DEFAULT 0          NOT NULL,
	Taxable  DECIMAL(18, 6) DEFAULT 0          NOT NULL,
	TaxRate  DECIMAL(18, 6) DEFAULT 0          NOT NULL,
	TaxDue   DECIMAL(18, 6) DEFAULT 0          NOT NULL,
	Other    DECIMAL(18, 6) DEFAULT 0          NOT NULL,
	Total    DECIMAL(18, 6) DEFAULT 0          NOT NULL
)
go

CREATE TRIGGER InvoiceHeader_UpdateOnlyActive
	ON InvoiceHeader
	FOR UPDATE , DELETE
	AS
BEGIN
	/*prevent updating invoices that are no longer in an active state
	,if an invoice is canceled or sent to a client, that invoice may no longer be changed/altered	  (Active = 0, Canceled = 1 & Invoiced = 2 )*/

	IF ((
		    SELECT IsPaid
		    FROM inserted
	    ) != 1)
			BEGIN
				IF (EXISTS( SELECT Id FROM deleted WHERE Status <> 0 ))
						BEGIN
							ROLLBACK
						END
			END
END;
go

CREATE TABLE dbo.Products
(
	Id          INT IDENTITY
		CONSTRAINT Products_pk
			PRIMARY KEY,
	Description VARCHAR(256)             NOT NULL,
	Price       DECIMAL(18, 6) DEFAULT 0 NOT NULL
)
go

CREATE TABLE dbo.InvoiceDetails
(
	Id              INT IDENTITY
		CONSTRAINT InvoiceDetails_pk
			PRIMARY KEY,
	InvoiceHeaderId INT           NOT NULL
		CONSTRAINT InvoiceDetails_InvoiceHeader_Id_fk
			REFERENCES dbo.InvoiceHeader
			ON DELETE CASCADE,
	ProductId       INT           NOT NULL
		CONSTRAINT InvoiceDetails_Products_Id_fk
			REFERENCES dbo.Products,
	Quantity        INT DEFAULT 1 NOT NULL,
	Taxed           BIT DEFAULT 0 NOT NULL
)
go

CREATE TRIGGER InvoiceDetails_UpdateOnlyActive
		ON InvoiceDetails
		FOR INSERT, UPDATE , DELETE
		AS
	BEGIN
		/*prevent updating invoices that are no longer in an active state,
		  if an invoice is canceled or sent to a client, that invoice may no longer be changed/altered
	  (Active = 0, Canceled = 1 & Invoiced = 2 )*/

		IF (EXISTS(
			--we need to get the state of the invoice from the header
			--for this to work on both insert and Update we will union the insert and delete tables header id's
			--we will then join the header table to check the state
				SELECT T.InvoiceHeaderId
				FROM (
					     SELECT InvoiceHeaderId
					     FROM deleted
					     UNION
					     SELECT InvoiceHeaderId
					     FROM inserted
				     )                            AS T
					     INNER JOIN InvoiceHeader AS IH
					                ON T.InvoiceHeaderId = IH.Id
				WHERE IH.Status <> 0
			))
				BEGIN
					ROLLBACK
				END
	END;
go

CREATE UNIQUE INDEX Products_Description_uindex
	ON dbo.Products ( Description )
go

CREATE   PROCEDURE [dbo].[Client_AddUpdate](
	                                                  @Id           NVARCHAR(128), @Name VARCHAR(256),
	                                                  @CompanyName  VARCHAR(256), @StreetAddress VARCHAR(256),
	                                                  @CityStateZip VARCHAR(MAX), @Phone NVARCHAR(256)
                                                  )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;
	BEGIN TRANSACTION
		BEGIN TRY
			MERGE dbo.Client AS C
			USING (
				      SELECT @Id, @Name, @CompanyName, @StreetAddress, @CityStateZip, @Phone
			      ) AS source( iid, Nam, CName, SAddress, CSZip, Pho )
			ON (C.Id = iid)
			WHEN MATCHED THEN
				UPDATE
				SET C.Name          = Nam
				  , C.CompanyName   = CName
				  , C.StreetAddress = SAddress
				  , C.CityStateZip  =CSZip
				  , C.Phone         = Pho
			WHEN NOT MATCHED THEN
				INSERT ( Name, CompanyName, StreetAddress, CityStateZip, Phone )
				VALUES ( Nam, CName, SAddress, CSZip, Pho );

			IF (@Id IS NOT NULL)
					BEGIN
						SELECT TOP (1)
							Id
							 , Name
							 , CompanyName
							 , StreetAddress
							 , CityStateZip
							 , Phone
						FROM dbo.Client
						WHERE Id = @Id
					END
				ELSE
					SELECT TOP (1)
						Id
						 , Name
						 , CompanyName
						 , StreetAddress
						 , CityStateZip
						 , Phone
					FROM dbo.Client
					WHERE Id = SCOPE_IDENTITY( )

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			DECLARE
				@error NVARCHAR(MAX)
				,@message NVARCHAR(MAX)
				,@xstate NVARCHAR(MAX);

			SELECT @error = ERROR_NUMBER( )
				 , @message = 'Client_AddUpdate :' + ERROR_MESSAGE( )
				 , @xstate = XACT_STATE( );

			ROLLBACK TRANSACTION;
			RAISERROR (@message, 16, 1);
		END CATCH;
END;
go

CREATE   PROCEDURE [dbo].[Client_Get](
	                                            @page INT = 0, @recPerPage INT =10
                                            )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	SELECT Id, Name, CompanyName, StreetAddress, CityStateZip, Phone
	FROM dbo.Client
	ORDER BY Id
	OFFSET (@page * @recPerPage) ROWS FETCH NEXT @recPerPage ROWS ONLY;
END;
go

CREATE   PROCEDURE [dbo].[Client_GetDetails](
	@Id INT
                                                   )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	SELECT TOP (1) Id, Name, CompanyName, StreetAddress, CityStateZip, Phone FROM dbo.Client WHERE Id = @Id
END;
go

CREATE   PROCEDURE [dbo].[Client_GetPageCount](
	@recPerPage INT = 10
                                                     )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	-- 	IF 0 = 1 SET FMTONLY OFF;

	-- for the CEILING function to work we need to work with DECIMAL instead of int
	DECLARE @recPerPageDec DECIMAL(18, 6) = CAST( @recPerPage AS DECIMAL(18, 6) );
	SELECT cast(CEILING( COUNT( Id ) / @recPerPageDec ) as INT) AS Count FROM dbo.Client;
END;
go

CREATE   PROCEDURE [dbo].[Clients_GetAll]
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;
	select Id, Name, CompanyName, StreetAddress, CityStateZip, Phone from Client
END;
go

CREATE   PROCEDURE [dbo].[InvoiceDetails_AddUpdate](
	                                                          @Id       INT, @InvoiceHeaderId INT, @ProductId INT,
	                                                          @Quantity INT, @Taxed BIT
                                                          )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;
	BEGIN TRANSACTION
		BEGIN TRY

			MERGE dbo.InvoiceDetails AS ID
			USING (
				      SELECT @InvoiceHeaderId, @ProductId, @Quantity, @Taxed
			      ) AS source( IHeaderId, PId, Quan, Tax )
			ON (ID.Id = @Id)
			WHEN MATCHED THEN
				UPDATE
				SET ID.InvoiceHeaderId = IHeaderId
				  , ID.ProductId       = PId
				  , ID.Quantity        = Quan
				  , ID.Taxed           = Tax
			WHEN NOT MATCHED THEN
				INSERT ( InvoiceHeaderId, ProductId, Quantity, Taxed ) VALUES ( IHeaderId, PId, Quan, Tax );

			SELECT TOP (1) Id, InvoiceHeaderId, ProductId, Quantity, Taxed
			FROM InvoiceDetails
			WHERE (Id = SCOPE_IDENTITY( )OR Id = @Id);

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			DECLARE
				@error NVARCHAR(MAX)
				,@message NVARCHAR(MAX)
				,@xstate NVARCHAR(MAX);

			SELECT @error = ERROR_NUMBER( )
				 , @message = 'InvoiceDetails_AddUpdate :' + ERROR_MESSAGE( )
				 , @xstate = XACT_STATE( );

			ROLLBACK TRANSACTION;
			RAISERROR (@message, 16, 1);
		END CATCH;
END;
go

CREATE   PROCEDURE [dbo].[InvoiceDetails_Get](
	@InvoiceHeaderId INT
                                                    )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
--IF 0 = 1 SET FMTONLY OFF;

	SELECT Id, InvoiceHeaderId, ProductId, Quantity, Taxed FROM InvoiceDetails WHERE InvoiceHeaderId = @InvoiceHeaderId
END;
go

CREATE   PROCEDURE [dbo].[Invoice_AddUpdate](
	                                                   @Id       INT,
	                                                   @Date     DATETIME2,
	                                                   @DueDate  DATETIME2,
	                                                   @ClientId INT,
	                                                   @Subtotal DECIMAL(18, 6),
	                                                   @Taxable  DECIMAL(18, 6),
	                                                   @TaxRate  DECIMAL(18, 6),
	                                                   @TaxDue   DECIMAL(18, 6),
	                                                   @Other    DECIMAL(18, 6),
	                                                   @Total    DECIMAL(18, 6)
                                                   )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;
	BEGIN TRANSACTION
		BEGIN TRY

			MERGE dbo.InvoiceHeader AS IH
			USING (
				      SELECT @Date
					       , @DueDate
					       , @ClientId
					       , @Subtotal
					       , @Taxable
					       , @TaxRate
					       , @TaxDue
					       , @Other
					       , @Total
			      ) AS source( D, DD, CId, SubT, Tax, TRate, TDue, Oth, T )
			ON (IH.Id = @Id)
			WHEN MATCHED THEN
				UPDATE
				SET IH.Date     = D
				  , IH.DueDate  = DD
				  , IH.ClientId = CId
				  , IH.Subtotal = SubT
				  , IH.Taxable  = Tax
				  , IH.TaxRate  = TRate
				  , IH.TaxDue   = TDue
				  , IH.Other    = Oth
				  , IH.Total    = T
			WHEN NOT MATCHED THEN
				INSERT ( Date, DueDate, ClientId, Subtotal, Taxable, TaxRate, TaxDue, Other, Total )
				VALUES ( D, DD, CId, SubT, Tax, TRate, TDue, Oth, T );

			SELECT Id, Date, Status, ClientId, DueDate, IsPaid, Subtotal, Taxable, TaxRate, TaxDue, Other, Total
			FROM InvoiceHeader
			WHERE (Id = SCOPE_IDENTITY( ) OR Id = @Id)

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			DECLARE
				@error NVARCHAR(MAX)
				,@message NVARCHAR(MAX)
				,@xstate NVARCHAR(MAX);

			SELECT @error = ERROR_NUMBER( )
				 , @message = 'Invoice_AddUpdate :' + ERROR_MESSAGE( )
				 , @xstate = XACT_STATE( );

			ROLLBACK TRANSACTION;
			RAISERROR (@message, 16, 1);
		END CATCH;
END;
go

CREATE   PROCEDURE [dbo].[Invoice_Get](
	                                               @page INT = 0, @recPerPage INT = 50
	                                             , @date DATETIME2, @clientId INT
                                             )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	SELECT Id, Date, Status, ClientId, DueDate, IsPaid, Subtotal, Taxable, TaxRate, TaxDue, Other, Total
	FROM InvoiceHeader
	WHERE (Date = @date OR @date IS NULL)
	  AND (ClientId = @clientId OR @clientId IS NULL)
	ORDER BY Id
	OFFSET (@page * @recPerPage) ROWS FETCH NEXT @recPerPage ROWS ONLY

END;
go

CREATE   PROCEDURE [dbo].[Invoice_GetDetails](
	@Id INT
                                                    )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	SELECT TOP (1) Id, Date, Status, ClientId, DueDate, IsPaid, Subtotal, Taxable, TaxRate, TaxDue, Other, Total
	FROM InvoiceHeader WHERE Id = @Id
END;
go

CREATE   PROCEDURE [dbo].[Invoice_GetPageCount](
	@recPerPage INT = 50
                                                      )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	-- for the CEILING function to work we need to work with DECIMAL instead of int
	DECLARE @recPerPageDec DECIMAL(18, 6) = CAST( @recPerPage AS DECIMAL(18, 6) );
	SELECT CAST( CEILING( COUNT( Id ) / @recPerPageDec ) AS INT ) AS Count FROM dbo.InvoiceHeader;
END;
go

CREATE   PROCEDURE [dbo].[Product_AddUpdate](
	                                                   @Id NVARCHAR(128), @Description VARCHAR(256), @Price VARCHAR(256)
                                                   )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;
	BEGIN TRANSACTION
		BEGIN TRY
			MERGE dbo.Products AS P
			USING (
				      SELECT @Id, @Description, @Price
			      ) AS source( iid, Des, Pri )
			ON (P.Id = iid)
			WHEN MATCHED THEN
				UPDATE
				SET P.Description = Des
				  , P.Price       = Pri
			WHEN NOT MATCHED THEN
				INSERT ( Description, Price )
				VALUES ( Des, Pri );


			IF (@Id IS NOT NULL)
					BEGIN
						SELECT TOP (1)
							Id
							 , Description
							 , Price
						FROM dbo.Products
						WHERE Id = @Id
					END
				ELSE
					SELECT TOP (1)
						Id
						 , Description
						 , Price
					FROM dbo.Products
					WHERE Id = SCOPE_IDENTITY( )

			COMMIT TRANSACTION;
		END TRY
		BEGIN CATCH
			DECLARE
				@error NVARCHAR(MAX)
				,@message NVARCHAR(MAX)
				,@xstate NVARCHAR(MAX);

			SELECT @error = ERROR_NUMBER( )
				 , @message = 'Client_AddUpdate :' + ERROR_MESSAGE( )
				 , @xstate = XACT_STATE( );

			ROLLBACK TRANSACTION;
			RAISERROR (@message, 16, 1);
		END CATCH;
END;
go

CREATE   PROCEDURE [dbo].[Product_Get](
	                                            @page INT = 0, @recPerPage INT =10
                                            )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	SELECT Id, Description, Price
	FROM dbo.Products
	ORDER BY Id
	OFFSET (@page * @recPerPage) ROWS FETCH NEXT @recPerPage ROWS ONLY;
END;
go

CREATE   PROCEDURE [dbo].[Product_GetAll]
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;
	select Id, Description, Price from Products
END;
go

CREATE   PROCEDURE [dbo].[Product_GetDetails](
	@Id INT
                                                   )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	--IF 0 = 1 SET FMTONLY OFF;

	SELECT TOP (1) Id, Description, Price FROM dbo.Products WHERE Id = @Id
END;
go

CREATE   PROCEDURE [dbo].[Product_GetPageCount](
	@recPerPage INT = 10
                                                     )
--WITH ENCRYPTION
AS
BEGIN
	SET NOCOUNT ON;
	-- 	IF 0 = 1 SET FMTONLY OFF;

	-- for the CEILING function to work we need to work with DECIMAL instead of int
	DECLARE @recPerPageDec DECIMAL(18, 6) = CAST( @recPerPage AS DECIMAL(18, 6) );
	SELECT cast(CEILING( COUNT( Id ) / @recPerPageDec ) as INT) AS Count FROM dbo.Products;
END;
go

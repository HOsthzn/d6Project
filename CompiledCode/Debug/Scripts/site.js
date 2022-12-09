const app =
{
    request(headers, path, method, queryString, payload ) {
        return new Promise( (resolved, rejected) => {
            //defaults
            headers = typeof headers === "object" && headers !== null
                      ? headers
                      : { };
            path = typeof path === "string"
                   ? path
                   : "/";
            method = typeof method === "string"
                     && [ "GET", "POST", "PUT", "DELETE" ].indexOf( method.toUpperCase( ) )
                     > -1
                     ? method.toUpperCase( )
                     : "GET";
            queryString = typeof queryString === "object" && queryString !== null
                          ? queryString
                          : { };
            payload = typeof payload === "object" && payload !== null
                      ? payload
                      : { };

            let requestUrl = path + "?";
            let count = 0;
            for( let key in queryString ) {
                if( queryString.hasOwnProperty( key ) ) {
                    count++;
                    if( count > 1 ) requestUrl += "&";
                    requestUrl += `${ key }=${ queryString[ key ] }`;
                }
            }

            const xhr = new XMLHttpRequest( );
            xhr.open( method, requestUrl, true );
            xhr.setRequestHeader( "Content-type", "application/json" );

            for( let key in headers ) if( headers.hasOwnProperty( key ) ) xhr.setRequestHeader( key, headers[ key ] );

            xhr.onreadystatechange = function( ) {
                try {
                    if( xhr.readyState === XMLHttpRequest.DONE ) {
                        const statusCode = xhr.status;
                        const response = xhr.responseText;

                        const result = JSON.parse( response );
                        resolved( { statusCode, result } );
                    }
                } catch( e ) {
                    rejected( e );
                }
            };

            xhr.send( JSON.stringify( payload ) );
        } );
    }
    , sessionStorage: {
        get: function( key ) {
            //get value from local storage
            key = this.key( key );
            return JSON.parse( sessionStorage.getItem( key ) );
        }
        , set: function( key, value ) {
            //set value in local storage
            key = this.key( key );
            sessionStorage.setItem( key, JSON.stringify( value ) );
            return this.get( key );
        }
        , remove: function( key ) {
            //remove value from local storage
            key = this.key( key );
            sessionStorage.removeItem( key );
        }
        , clear: function( ) {
            //clear the local storage
            sessionStorage.clear( );
        }
        , Exists: function( key ) {
            //check if value exists in local storage
            key = this.key( key );
            return this.get( key ) !== null;
        }
        , key: function( key ) {
            //transform value to valid key
            key = typeof key === "string" && key.trim( ).length > 0
                  ? key
                  : false;
            if( key && key !== "#" ) return key.toLowerCase( ).replace( /\s/g, "" );
            else throw new Error( "invalid key value" );
        }
    }
    , Guid: () => ( [ 1e7 ] + -1e3 + -4e3 + -8e3 + -1e11 ).replace( /[018]/g
        , c => ( c
            ^ crypto.getRandomValues(
                new Uint8Array( 1 ) )[ 0 ]
            & 15 >> c / 4 ).toString( 16 ) )
};

const clients = {
    pageNumber: 0
    , pageCount: 0
    , init() {
        const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
        ddlRecPerPage.addEventListener( "change", clients.recordsPerPage );

        clients.GetClients( clients.pageNumber, parseInt( ddlRecPerPage.value ) );

        document.getElementById( "btnPrevious" ).addEventListener( "click", clients.pagination.previous );
        document.getElementById( "btnNext" ).addEventListener( "click", clients.pagination.next );
        document.querySelector( ".pagination" ).addEventListener( "click", clients.pagination.selectPage );
    }
    , GetClients(page, recPerPage) {
        app.request( undefined, "/Client/GetClients", "GET", { page, recPerPage }, undefined )
            .then( ({ statusCode, result }) => {
                if( statusCode === 200 ) {
                    const tbClients = document.getElementById( "tbClients" );
                    //clear tBody
                    tbClients.tBodies[ 0 ].innerHTML = "";
                    let altRowCount = 0;
                    for( const client of result.Clients ) {
                        //add row to table
                        const row = tbClients.tBodies[ 0 ].insertRow( );

                        //add class to alternating rows
                        altRowCount++;
                        if( ( altRowCount % 2 ) === 0 ) row.classList.add( "altRow" );

                        //add cells to row
                        for( const key in client )
                            if( client.hasOwnProperty( key ) )
                                if( key !== "Id" ) row.insertCell( ).innerText = client[ key ];

                        // record btn's (Edit,Details,Delete)
                        const btnEdit = btn( "Edit", `/Client/Edit/${ client.Id }`, "btn", "btn-primary" );
                        const btnDetails = btn( "Details", `/Client/Details/${ client.Id }`, "btn", "btn-info" );
                        const btnDelete = btn( "Delete", `/Client/Delete/${ client.Id }`, "btn", "btn-danger" );

                        row.insertCell( ).innerHTML =
                            `${ btnEdit.outerHTML } | ${ btnDetails.outerHTML } | ${ btnDelete.outerHTML }`;
                    }

                    clients.pagination.PageCount( result.PageCount );

                    function btn( text, href, ...classItems ) {
                        const result = document.createElement( "a" );
                        classItems.forEach( c => result.classList.add( c ) );
                        result.href = href;
                        result.innerText = text;
                        return result;
                    }
                }
            } )
            .catch( err => console.log( err ) );
    }
    , pagination: {
        next() {
            if( clients.pageNumber < clients.pageCount - 1 ) {
                const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
                clients.pageNumber++;
                clients.GetClients( clients.pageNumber, parseInt( ddlRecPerPage.value ) );
            }
        }
        , previous() {
            if( clients.pageNumber > 0 ) {
                const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
                clients.pageNumber--;
                clients.GetClients( clients.pageNumber, parseInt( ddlRecPerPage.value ) );
            }
        }
        , selectPage(e) {
            const parentElement = e.target.parentElement;
            const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );

            //could also have used indexOf >= 1 (older browsers doesn't support includes)
            if( [ "btnPrevious", "btnNext" ].includes( parentElement.id ) ) return;
            clients.pageNumber = parentElement.value;
            clients.GetClients( clients.pageNumber, parseInt( ddlRecPerPage.value ) );
        }
        , PageCount(numOfPages) {
            const btnNext = document.getElementById( "btnNext" );

            clients.pageCount = numOfPages;

            document.querySelectorAll( ".pageNum" ).forEach( pn => pn.remove( ) );
            for( let i = 1; i <= numOfPages; i++ ) {

                const a = document.createElement( "a" );
                a.classList.add( "page-link" );
                a.href = "#";
                a.innerText = i;

                const li = document.createElement( "li" );
                li.classList.add( "page-item", "pageNum" );
                li.appendChild( a );
                li.value = i - 1;

                if( li.value === clients.pageNumber ) li.classList.add( "active" );

                btnNext.insertAdjacentHTML( "beforebegin", li.outerHTML );
            }
        }
    }
    , recordsPerPage(e) { clients.GetClients( clients.pageNumber, parseInt( e.target.value ) ); }
};

const products = {
    pageNumber: 0
    , pageCount: 0
    , init() {
        const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
        ddlRecPerPage.addEventListener( "change", products.recordsPerPage );

        products.GetProducts( products.pageNumber, parseInt( ddlRecPerPage.value ) );

        document.getElementById( "btnPrevious" ).addEventListener( "click", products.pagination.previous );
        document.getElementById( "btnNext" ).addEventListener( "click", products.pagination.next );
        document.querySelector( ".pagination" ).addEventListener( "click", products.pagination.selectPage );
    }
    , GetProducts(page, recPerPage) {
        app.request( undefined, "/Product/GetProducts", "GET", { page, recPerPage }, undefined )
            .then( ({ statusCode, result }) => {
                if( statusCode === 200 ) {
                    const tbProducts = document.getElementById( "tbProducts" );
                    //clear tBody
                    tbProducts.tBodies[ 0 ].innerHTML = "";
                    let altRowCount = 0;
                    for( const product of result.Products ) {
                        //add row to table
                        const row = tbProducts.tBodies[ 0 ].insertRow( );

                        //add class to alternating rows
                        altRowCount++;
                        if( ( altRowCount % 2 ) === 0 ) row.classList.add( "altRow" );

                        //add cells to row
                        for( const key in product )
                            if( product.hasOwnProperty( key ) )
                                if( key !== "Id" ) row.insertCell( ).innerText = product[ key ];

                        // record btn's (Edit,Details,Delete)
                        const btnEdit = btn( "Edit", `/Product/Edit/${ product.Id }`, "btn", "btn-primary" );
                        const btnDetails = btn( "Details", `/Product/Details/${ product.Id }`, "btn", "btn-info" );
                        const btnDelete = btn( "Delete", `/Product/Delete/${ product.Id }`, "btn", "btn-danger" );

                        row.insertCell( ).innerHTML =
                            `${ btnEdit.outerHTML } | ${ btnDetails.outerHTML } | ${ btnDelete.outerHTML }`;
                    }

                    products.pagination.PageCount( result.PageCount );

                    function btn( text, href, ...classItems ) {
                        const result = document.createElement( "a" );
                        classItems.forEach( c => result.classList.add( c ) );
                        result.href = href;
                        result.innerText = text;

                        return result;
                    }
                }
            } )
            .catch( err => console.log( err ) );
    }
    , pagination: {
        next() {
            if( products.pageNumber < products.pageCount - 1 ) {
                const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
                products.pageNumber++;
                products.GetProducts( products.pageNumber, parseInt( ddlRecPerPage.value ) );
            }
        }
        , previous() {
            if( products.pageNumber > 0 ) {
                const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
                products.pageNumber--;
                products.GetProducts( pageNumber.pageNumber, parseInt( ddlRecPerPage.value ) );
            }
        }
        , selectPage(e) {
            const parentElement = e.target.parentElement;
            const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );

            //could also have used indexOf >= 1 (older browsers doesn't support includes)
            if( [ "btnPrevious", "btnNext" ].includes( parentElement.id ) ) return;
            products.pageNumber = parentElement.value;
            products.GetProducts( products.pageNumber, parseInt( ddlRecPerPage.value ) );
        }
        , PageCount(numOfPages) {
            const btnNext = document.getElementById( "btnNext" );

            products.pageCount = numOfPages;

            document.querySelectorAll( ".pageNum" ).forEach( pn => pn.remove( ) );
            for( let i = 1; i <= numOfPages; i++ ) {

                const a = document.createElement( "a" );
                a.classList.add( "page-link" );
                a.href = "#";
                a.innerText = i;

                const li = document.createElement( "li" );
                li.classList.add( "page-item", "pageNum" );
                li.appendChild( a );
                li.value = i - 1;

                if( li.value === products.pageNumber ) li.classList.add( "active" );

                btnNext.insertAdjacentHTML( "beforebegin", li.outerHTML );
            }
        }
    }
    , recordsPerPage(e) { products.GetProducts( products.pageNumber, parseInt( e.target.value ) ); }
};

const invoices = {
    pageNumber: 0
    , pageCount: 0
    , init() {
        const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
        ddlRecPerPage.addEventListener( "change", invoices.recordsPerPage );

        invoices.GetInvoices( invoices.pageNumber, parseInt( ddlRecPerPage.value ) );

        document.getElementById( "btnPrevious" ).addEventListener( "click", invoices.pagination.previous );
        document.getElementById( "btnNext" ).addEventListener( "click", invoices.pagination.next );
        document.querySelector( ".pagination" ).addEventListener( "click", invoices.pagination.selectPage );

        //add filter events
        document.getElementById( "dpDate" ).addEventListener( "change", invoices.filters.dateChange );
        document.getElementById( "ClientId" ).addEventListener( "change", invoices.filters.ClientIdChange );
    }
    , GetInvoices(page, recPerPage, date, clientId) {
        app.request( undefined, "/Invoice/GetInvoices", "GET", { page, recPerPage, date, clientId }, undefined )
            .then( ({ statusCode, result }) => {
                if( statusCode === 200 ) {
                    const tbInvoices = document.getElementById( "tbInvoices" );

                    //clear tBody
                    tbInvoices.tBodies[ 0 ].innerHTML = "";
                    let altRowCount = 0;
                    for( const invoice of result.Invoices ) {
                        //add row to table
                        const row = tbInvoices.tBodies[ 0 ].insertRow( );

                        //add class to alternating rows
                        altRowCount++;
                        if( ( altRowCount % 2 ) === 0 ) row.classList.add( "altRow" );

                        //add cells to row
                        for( const key in invoice ) {
                            if( invoice.hasOwnProperty( key ) ) {
                                if( key === "Status" ) {
                                    switch( invoice[ key ] ) {
                                        case 0:
                                            row.insertCell( ).innerText = "Active";
                                            break;
                                        case 1:
                                            row.insertCell( ).innerText = "Canceled";
                                            break;
                                        case 2:
                                            row.insertCell( ).innerText = "Invoiced";
                                            break;
                                    }
                                } else row.insertCell( ).innerText = invoice[ key ];

                            }
                        }

                        let btnHtml = "";

                        // record btn's (Edit,Invoice,Cancel)
                        //Edit & Cancel is only available if the invoice is still in active state (State = 0)
                        const btnCell = row.insertCell( );

                        if( invoice.Status === 0 ) {
                            btnCell.innerHTML += `${ btn( "Edit"
                                    , `/Invoice/Edit/${ invoice.Id }`
                                    , "btn"
                                    , "btn-primary" )
                                .outerHTML } | `;
                        }

                        btnCell.innerHTML += btn( "Invoice", `/Invoice/Details/${ invoice.Id }`, "btn", "btn-info" )
                            .outerHTML;
                    }

                    products.pagination.PageCount( result.PageCount );

                    function btn( text, href, ...classItems ) {
                        const result = document.createElement( "a" );
                        classItems.forEach( c => result.classList.add( c ) );
                        result.href = href;
                        result.innerText = text;

                        return result;
                    }
                }
            } )
            .catch( err => console.log( err ) );
    }
    , filters: {
        dateChange(e) {
            const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
            const clientId = document.getElementById( "ClientId" );
            invoices.GetInvoices( invoices.pageNumber
                , parseInt( ddlRecPerPage.value )
                , e.target.value
                , clientId[ clientId.selectedIndex ].value );
        }
        , ClientIdChange(e) {
            const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
            const dpDate = document.getElementById( "dpDate" );
            invoices.GetInvoices( invoices.pageNumber
                , parseInt( ddlRecPerPage.value )
                , dpDate.value
                , e.target[ e.target.selectedIndex ].value );
        }
    }
    , pagination: {
        next() {
            if( invoices.pageNumber < invoices.pageCount - 1 ) {
                const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
                invoices.pageNumber++;
                invoices.GetInvoices( invoices.pageNumber, parseInt( ddlRecPerPage.value ) );
            }
        }
        , previous() {
            if( invoices.pageNumber > 0 ) {
                const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );
                invoices.pageNumber--;
                invoices.GetInvoices( pageNumber.pageNumber, parseInt( ddlRecPerPage.value ) );
            }
        }
        , selectPage(e) {
            const parentElement = e.target.parentElement;
            const ddlRecPerPage = document.getElementById( "ddlRecPerPage" );

            //could also have used indexOf >= 1 (older browsers doesn't support includes)
            if( [ "btnPrevious", "btnNext" ].includes( parentElement.id ) ) return;
            invoices.pageNumber = parentElement.value;
            invoices.GetInvoices( invoices.pageNumber, parseInt( ddlRecPerPage.value ) );
        }
        , PageCount(numOfPages) {
            const btnNext = document.getElementById( "btnNext" );

            invoices.pageCount = numOfPages;

            document.querySelectorAll( ".pageNum" ).forEach( pn => pn.remove( ) );
            for( let i = 1; i <= numOfPages; i++ ) {

                const a = document.createElement( "a" );
                a.classList.add( "page-link" );
                a.href = "#";
                a.innerText = i;

                const li = document.createElement( "li" );
                li.classList.add( "page-item", "pageNum" );
                li.appendChild( a );
                li.value = i - 1;

                if( li.value === invoices.pageNumber ) li.classList.add( "active" );

                btnNext.insertAdjacentHTML( "beforebegin", li.outerHTML );
            }
        }
    }
    , recordsPerPage(e) { invoices.GetInvoices( invoices.pageNumber, parseInt( e.target.value ) ); }
    , CreateEdit: {
        init() {
            invoices.CreateEdit.invoiceDetails.calc.init( );

            document.getElementById( "ClientId" ).addEventListener( "change", invoices.CreateEdit.Customer.change );
            document.getElementById( "btnAddItem" )
                .addEventListener( "click", invoices.CreateEdit.invoiceDetails.addNewItem );

            app.request( undefined, "/Product/GetAll", "GET", undefined, undefined )
                .then( ({ statusCode, result }) => {
                    if( statusCode === 200 ) app.sessionStorage.set( "products", result );
                } )
                .catch( err => console.log( err ) );

            document.getElementById( "TaxRate" )
                .addEventListener( "change", invoices.CreateEdit.invoiceDetails.calc.changeTaxRate );
            document.getElementById( "Other" )
                .addEventListener( "change", invoices.CreateEdit.invoiceDetails.calc.changeOther );

            document.querySelectorAll( ".ddlProduct" )
                .forEach( el => {
                    el.addEventListener( "change", invoices.CreateEdit.invoiceDetails.product.change )
                } );
            document.querySelectorAll( ".quantity" )
                .forEach( el => {
                    el.addEventListener( "change", invoices.CreateEdit.invoiceDetails.quantity.change )
                } );
            document.querySelectorAll( ".taxed" )
                .forEach( el => { el.addEventListener( "change", invoices.CreateEdit.invoiceDetails.taxed.change ) } );
        }
        , Customer: {
            change(e) {
                const selectedCustomer = e.target.options[ e.target.selectedIndex ];
                app.request( undefined, "/Client/GetClientDetails", "GET", { id: selectedCustomer.value }, undefined )
                    .then( ({ statusCode, result }) => {
                        if( statusCode === 200 ) {
                            setValues( {
                                Name: result.Name
                                , CompanyName: result.CompanyName
                                , StreetAddress: result.StreetAddress
                                , CityStateZip: result.CityStateZip
                                , Phone: result.Phone
                            } );
                        }
                    } )
                    .catch( err => {
                        console.log( err );

                        setValues( {
                            Name: null
                            , CompanyName: null
                            , StreetAddress: null
                            , CityStateZip: null
                            , Phone: null
                        } );
                    } );

                function setValues( { Name, CompanyName, StreetAddress, CityStateZip, Phone } ) {
                    document.getElementById( "lblBillTo_Name" ).innerText = Name;
                    document.getElementById( "lblBillTo_CompanyName" ).innerText = CompanyName;
                    document.getElementById( "lblBillTo_StreetAddress" ).innerText = StreetAddress;
                    document.getElementById( "lblBillTo_CityStateZip" ).innerText = CityStateZip;
                    document.getElementById( "lblBillTo_Phone" ).innerText = Phone;
                }
            }
        }
        , invoiceDetails: {
            calc: {
                init() {
                    this.subtotal = this.toFloat( document.getElementById( "Subtotal" ).value );
                    this.taxable = this.toFloat( document.getElementById( "Taxable" ).value );
                    this.taxDue = this.toFloat( document.getElementById( "TaxDue" ).value );
                    this.other = this.toFloat( document.getElementById( "Other" ).value );
                    this.total = this.toFloat( document.getElementById( "Total" ).value );
                }
                , subtotal: 0
                , taxable: 0
                , taxRate ( ) { return document.getElementById( "TaxRate" ).value }
                , taxDue: 0
                , other: 0
                , total: 0
                , addSubTotal(val, taxed) {
                    const float = parseFloat( val );
                    if( !isNaN( float ) ) {
                        this.subtotal += float;
                        if( taxed ) this.addTax( val );
                        this.calcTotal( );
                    }
                }
                , subtractSubTotal(val, taxed) {
                    const float = parseFloat( val );
                    if( !isNaN( float ) ) {
                        this.subtotal -= float;
                        if( taxed ) this.subtractTax( val );
                        this.calcTotal( );
                    }
                }
                , addTax(val) {
                    this.taxable += this.toFloat( val );
                    this.calcTax( );
                }
                , subtractTax(val) {
                    this.taxable -= this.toFloat( val );
                    this.calcTax( );
                }
                , calcTax() {
                    //Number.EPSILON is used in this calc to make sure the rounding of values to 2 des is always correct
                    this.taxDue = Math.round( ( ( ( this.taxable / 100 ) * this.taxRate( ) ) + Number.EPSILON ) * 100 )
                        / 100;
                }
                , calcTotal() {
                    this.total = this.subtotal + this.taxDue + this.other;
                    this.setTotals( );
                }
                , setTotals() {
                    document.getElementById( "Subtotal" ).value = this.subtotal;
                    document.getElementById( "Taxable" ).value = this.taxable;
                    document.getElementById( "TaxDue" ).value = this.taxDue;
                    document.getElementById( "Total" ).value = this.total;
                }
                , changeTaxRate(e) {
                    invoices.CreateEdit.invoiceDetails.calc.calcTax( );
                    invoices.CreateEdit.invoiceDetails.calc.calcTotal( );
                }
                , changeOther(e) {
                    invoices.CreateEdit.invoiceDetails.calc.other = e.target.value;
                    invoices.CreateEdit.invoiceDetails.calc.calcTotal( );
                }
                , toFloat(val) {
                    const float = parseFloat( val );
                    if( !isNaN( float ) ) return float;
                    return 0;
                }
            }
            , addNewItem(e) {
                const tbInvoicesDetails = document.getElementById( "tbInvoicesDetails" );

                const row = tbInvoicesDetails.tBodies[ 0 ].insertRow( );

                //so the row may be identifiable for removal
                row.id = app.Guid( );

                //remove
                const d = row.insertCell( );
                //this field is to group and index the list of items from model binding
                d.innerHTML += `<input name='InvoiceDetails.Index' type='hidden' value="${
                    tbInvoicesDetails.tBodies[ 0 ].rows.length }" />`;
                d.innerHTML +=
                    `<input class="btn btn-secondary" type="button" value="remove" onclick="invoices.CreateEdit.invoiceDetails.removeItem(this)"/>`;

                //Description
                const description = row.insertCell( );
                description.innerHTML =
                    invoices.CreateEdit.invoiceDetails.product.html( tbInvoicesDetails.tBodies[ 0 ].rows.length );

                const products = app.sessionStorage.get( "products" );
                const ddl = document.getElementById(
                    `InvoiceDetails_${ tbInvoicesDetails.tBodies[ 0 ].rows.length }_ProductId` );
                for( const product of products ) {
                    const option = document.createElement( "option" );
                    option.value = product.Id;
                    option.innerText = product.Description;
                    option.dataset.price = product.Price;
                    ddl.appendChild( option );
                }

                description.addEventListener( "change", invoices.CreateEdit.invoiceDetails.product.change );

                //Quantity
                const quantity = row.insertCell( );
                quantity.innerHTML =
                    invoices.CreateEdit.invoiceDetails.quantity.html( tbInvoicesDetails.tBodies[ 0 ].rows.length );

                quantity.addEventListener( "change", invoices.CreateEdit.invoiceDetails.quantity.change );

                //Taxed
                const taxed = row.insertCell( );
                taxed.innerHTML =
                    invoices.CreateEdit.invoiceDetails.taxed.html( tbInvoicesDetails.tBodies[ 0 ].rows.length );

                taxed.addEventListener( "change", invoices.CreateEdit.invoiceDetails.taxed.change );

                //Amount
                const amount = row.insertCell( );
                amount.innerHTML = `<input class="amount" disabled/>`;
            }
            , removeItem(e) {
                document.getElementById( e.parentElement.parentElement.id ).remove( );

                const amount = e.parentElement.parentElement.querySelector( "input.amount" );
                const taxed = e.parentElement.parentElement.querySelector( "input[type=checkbox]" );

                invoices.CreateEdit.invoiceDetails.calc.subtractSubTotal( amount.value, taxed.checked );
            }
            , product: {
                html(index) {
                    return `<select id="InvoiceDetails_${ index }_ProductId" class="ddlProduct"  name="InvoiceDetails[${
                        index }].ProductId" ><option value>Select Product</option></select>`;
                }
                , change(e) {
                    const { amount, quantity, taxed, price } = invoices.CreateEdit.invoiceDetails.getRowValues( e );
                    invoices.CreateEdit.invoiceDetails.calc.subtractSubTotal( amount.value, taxed );

                    amount.value = price * quantity;
                    invoices.CreateEdit.invoiceDetails.calc.addSubTotal( amount.value, taxed );
                }
            }
            , quantity: {
                html(index) {
                    return `<input class="quantity" id="InvoiceDetails_${ index }_Quantity" name="InvoiceDetails[${
                        index }].Quantity" type="number" min="1" value="1"/>`;
                }
                , change(e) {
                    const { amount, quantity, taxed, price } = invoices.CreateEdit.invoiceDetails.getRowValues( e );
                    invoices.CreateEdit.invoiceDetails.calc.subtractSubTotal( amount.value, taxed );

                    amount.value = price * quantity;
                    invoices.CreateEdit.invoiceDetails.calc.addSubTotal( amount.value, taxed );
                }
            }
            , taxed: {
                html(index) {
                    return `<input class="taxed" type="checkbox" id="InvoiceDetails_${ index
                        }_Taxed" /><input type="hidden" name="InvoiceDetails[${ index }].Taxed" value="false" />`;
                }
                , change(e) {
                    const { amount, taxed } = invoices.CreateEdit.invoiceDetails.getRowValues( e );
                    if( taxed ) {
                        e.target.nextElementSibling.value = true;
                        invoices.CreateEdit.invoiceDetails.calc.addTax( amount.value, taxed );
                        invoices.CreateEdit.invoiceDetails.calc.calcTotal( );
                    } else {
                        e.target.nextElementSibling.value = false;
                        invoices.CreateEdit.invoiceDetails.calc.subtractTax( amount.value, taxed );
                        invoices.CreateEdit.invoiceDetails.calc.calcTotal( );
                    }
                }
            }
            , getRowValues(e) {
                const parentElement = e.target.parentElement.parentElement;
                const amount = parentElement.querySelector( "input.amount" );
                const quantity = parentElement.querySelector( "input.quantity" );
                const taxed = parentElement.querySelector( "input[type=checkbox]" );
                const product = parentElement.querySelector( ".ddlProduct" );
                return {
                    amount
                    , quantity: quantity.value
                    , taxed: taxed.checked
                    , price: parseFloat( product[ product.selectedIndex ].dataset.price )
                }
            }
        }
    }
};

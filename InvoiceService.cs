using InvoiceAPI.Models;
using InvoiceAPIv2.Models.Data;
using InvoiceAPIv2.Models.View;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using QC = Microsoft.Data.SqlClient;

namespace InvoiceAPIv2
{
    public class InvoiceService
    {
        public async Task<InvoiceDetailViewModel> AddInvoice(InputModel input)
        {
            int lastInsertedId;

            using (var connection = new QC.SqlConnection(
                "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                "Database = invoice;" +
                "Trusted_Connection=True; TrustServerCertificate=True;"
                ))
            {
                await connection.OpenAsync();

                lastInsertedId = insertClient(input, connection);
                lastInsertedId = insertInvoice(input, connection, lastInsertedId);
                insertItems(input, connection, lastInsertedId);

                CalculateTotalFees(connection, lastInsertedId);

            }

            return await GetInvoice(lastInsertedId.ToString());
        }

        public async Task<List<InvoiceSummaryViewModel>> GetInvoices()
        {
            List<InvoiceSummaryViewModel> invoiceSummaries = new List<InvoiceSummaryViewModel>();

            using (var connection = new QC.SqlConnection(
                 "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                 "Database = invoice;" +
                 "Trusted_Connection=True; TrustServerCertificate=True;"
                 ))
            {

                int invoiceId;
                string clientName;
                DateTime paymentDueDate;
                int totalFees;
                string status;


                await connection.OpenAsync();

                var getInvoiceCommand = connection.CreateCommand();
                getInvoiceCommand.CommandText = @"
                SELECT InvoiceID, PaymentDueDate, Status, Clients.Name, Invoices.TotalFees
                FROM Invoices
                JOIN Clients ON Clients.ClientID = Invoices.ClientID;";

                //ApplicationUser applicationUser = await _userManager.GetUserAsync(httpContext.User);
                //int userId = (int)applicationUser?.Id;
                //getInvoiceCommand.Parameters.AddWithValue("@userId", userId);

                using var reader = await getInvoiceCommand.ExecuteReaderAsync();
                while (reader.Read())
                {
                    invoiceId = Int32.Parse(reader[0].ToString());
                    paymentDueDate = (DateTime)reader[1];
                    status = reader[2].ToString();
                    clientName = reader[3].ToString();
                    totalFees = Int32.Parse(reader[4].ToString());

                    InvoiceSummaryViewModel invoiceSummary = new InvoiceSummaryViewModel();
                    invoiceSummary.Id = invoiceId;
                    invoiceSummary.InvoiceDueDate = paymentDueDate;
                    invoiceSummary.Status = status;
                    invoiceSummary.ClientName = clientName;
                    invoiceSummary.TotalFees = totalFees;


                    invoiceSummaries.Add(invoiceSummary);

                }

                reader.Close();
            }

            return invoiceSummaries;
        }

        public async Task<InvoiceDetailViewModel> GetInvoice(string id)
        {
            InvoiceDetailViewModel invoice = new InvoiceDetailViewModel();

            using (var connection = new QC.SqlConnection(
                 "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                 "Database = invoice;" +
                 "Trusted_Connection=True; TrustServerCertificate=True;"
                 ))
            {
                await connection.OpenAsync();

                var getInvoiceCommand = connection.CreateCommand();
                getInvoiceCommand.CommandText = @"
                SELECT Invoices.InvoiceID, Invoices.Description, Invoices.Date, Invoices.PaymentTerms,
                Invoices.PaymentDueDate, Invoices.TotalFees, Invoices.Status, Invoices.BillFromAddress,
                Invoices.BillFromCity, Invoices.BillFromCountry, Invoices.BillFromPostal, Clients.Name,
                Clients.Address, Clients.City, Clients.Country, Clients.PostalCode,
                Clients.Email
                FROM Invoices
                JOIN Clients
                ON Invoices.ClientID = Clients.ClientID
                WHERE Invoices.InvoiceID = @InvoiceID;";

                getInvoiceCommand.Parameters.AddWithValue("@InvoiceID", id);
                using var reader = getInvoiceCommand.ExecuteReader();

                while (reader.Read())
                {
                    invoice.InvoiceId = Int32.Parse(reader[0].ToString());
                    invoice.Description = reader[1].ToString();
                    invoice.InvoiceDate = (DateTime)reader[2];
                    invoice.PaymentTerms = reader[3].ToString();
                    invoice.PaymentDue = (DateTime)reader[4];
                    invoice.TotalFees = Int32.Parse(reader[5].ToString());
                    invoice.Status = reader[6].ToString();
                    invoice.BillFromAddress = reader[7].ToString();
                    invoice.BillFromCity = reader[8].ToString();
                    invoice.BillFromCountry = reader[9].ToString();
                    invoice.BillFromPostal = reader[10].ToString();
                    invoice.Client.ClientName = reader[11].ToString();
                    invoice.Client.ClientAddress = reader[12].ToString();
                    invoice.Client.ClientCity = reader[13].ToString();
                    invoice.Client.ClientCountry = reader[14].ToString();
                    invoice.Client.ClientPostal = reader[15].ToString();
                    invoice.Client.ClientEmail = reader[16].ToString();
                }

                reader.Close();

                var getItemsCommand = connection.CreateCommand();
                getItemsCommand.CommandText = @"
                SELECT Items.Name, Items.Price, Items.Quantity
                FROM Invoices
                JOIN Items
                ON Invoices.InvoiceID = Items.InvoiceID
                WHERE Invoices.InvoiceID = @InvoiceID;";

                getItemsCommand.Parameters.AddWithValue("@InvoiceID", id);
                using var readerItems = getItemsCommand.ExecuteReader();

                while (readerItems.Read())
                {
                    Item item = new Item();
                    item.Name = readerItems[0].ToString();
                    item.Price = Int32.Parse(readerItems[1].ToString());
                    item.Quantity = Int32.Parse(readerItems[2].ToString());
                    invoice.Items.Add(item);
                }

                readerItems.Close();
            }


            return invoice;
        }

        public async void EditInvoice(InputModel input, string invoiceId)
        {
            using (var connection = new QC.SqlConnection(
                 "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                 "Database = invoice;" +
                 "Trusted_Connection=True; TrustServerCertificate=True;"
                 ))
            {
                await connection.OpenAsync();
                updateInvoice(input, connection, Int32.Parse(invoiceId));
                updateClient(input, connection, Int32.Parse(invoiceId));
                updateItems(input, connection, Int32.Parse(invoiceId));
                CalculateTotalFees(connection, Int32.Parse(invoiceId));
            }

        }

        public void DeleteInvoice(string id)
        {

        }


        public int insertClient(InputModel input, QC.SqlConnection connection)
        {
            QC.SqlParameter parameter;

            var insertClientCommand = connection.CreateCommand();
            insertClientCommand.CommandText = @"
                INSERT INTO Clients(Name, Email, Address, City, Country, PostalCode)
                VALUES(@Name, @Email, @Address, @City, @Country, @PostalCode)";

            parameter = new QC.SqlParameter("@Name", SqlDbType.VarChar, 25);
            parameter.Value = input.Client.ClientName;
            insertClientCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@Email", SqlDbType.VarChar, 50);
            parameter.Value = input.Client.ClientEmail;
            insertClientCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@Address", SqlDbType.VarChar, 75);
            parameter.Value = input.Client.ClientAddress;
            insertClientCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@City", SqlDbType.VarChar, 50);
            parameter.Value = input.Client.ClientCity;
            insertClientCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@Country", SqlDbType.VarChar, 50);
            parameter.Value = input.Client.ClientCountry;
            insertClientCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@PostalCode", SqlDbType.VarChar, 10);
            parameter.Value = input.Client.ClientPostal;
            insertClientCommand.Parameters.Add(parameter);

            insertClientCommand.ExecuteNonQuery();

            var selectLastIdCommand = connection.CreateCommand();
            string lastInsertedId = "";
            selectLastIdCommand.CommandText = @"SELECT @@IDENTITY;";

            using var reader = selectLastIdCommand.ExecuteReader();
            while (reader.Read())
            {
                lastInsertedId = reader[0].ToString();
            }

            reader.Close();

            return Int32.Parse(lastInsertedId);
        }


        public int insertInvoice(InputModel input, QC.SqlConnection connection, int lastInsertedId)
        {
            QC.SqlParameter parameter;

            var insertInvoiceCommand = connection.CreateCommand();
            insertInvoiceCommand.CommandText = @"
                INSERT INTO Invoices(ClientID,
                Description, Date, PaymentTerms, PaymentDueDate, TotalFees, BillFromAddress, BillFromCity, BillFromCountry, BillFromPostal)
                VALUES(@ClientID, @Description, @Date, @PaymentTerms, @PaymentDueDate, @TotalFees, @BillFromAddress, @BillFromCity,
                @BillFromCountry,  @BillFromPostal)";




            //Determine the payment due date
            DateTime paymentDueDate = CalculateDueDate(input.PaymentTerms, input.InvoiceDate);

            //Getting the user id
            //ApplicationUser applicationUser = await _userManager.GetUserAsync(httpContext.User);
            //int userId = (int)applicationUser?.Id;

            parameter = new QC.SqlParameter("@ClientID", SqlDbType.Int);
            parameter.Value = lastInsertedId;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@Description", SqlDbType.VarChar, 50);
            parameter.Value = input.Description;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@Date", SqlDbType.Date);
            parameter.Value = input.InvoiceDate;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@PaymentTerms", SqlDbType.VarChar, 50);
            parameter.Value = input.PaymentTerms;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@PaymentDueDate", SqlDbType.Date);
            parameter.Value = paymentDueDate;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@TotalFees", SqlDbType.Float);
            parameter.Value = 0;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@BillFromAddress", SqlDbType.VarChar, 50);
            parameter.Value = input.BillFromAddress;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@BillFromCity", SqlDbType.VarChar, 25);
            parameter.Value = input.BillFromCity;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@BillFromCountry", SqlDbType.VarChar, 25);
            parameter.Value = input.BillFromCountry;
            insertInvoiceCommand.Parameters.Add(parameter);

            parameter = new QC.SqlParameter("@BillFromPostal", SqlDbType.VarChar, 10);
            parameter.Value = input.BillFromPostal;
            insertInvoiceCommand.Parameters.Add(parameter);

            insertInvoiceCommand.ExecuteNonQuery();


            var selectLastIdCommand = connection.CreateCommand();
            selectLastIdCommand.CommandText = @"SELECT @@IDENTITY";

            using var reader = selectLastIdCommand.ExecuteReader();
            while (reader.Read())
            {
                lastInsertedId = Int32.Parse(reader[0].ToString());
            }

            reader.Close();

            return lastInsertedId;

        }


        public void insertItems(InputModel input, QC.SqlConnection connection, int lastInsertedId)
        {
            foreach (var item in input.Items)
            {
                var addItemCommand = connection.CreateCommand();
                addItemCommand.CommandText = @"
                    INSERT INTO Items(InvoiceID,
                    Name, Quantity, Price)VALUES(@InvoiceID, @Name, @Quantity, @Price);";

                addItemCommand.Parameters.AddWithValue("@InvoiceID", lastInsertedId);
                addItemCommand.Parameters.AddWithValue("@Name", item.Name);
                addItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                addItemCommand.Parameters.AddWithValue("Price", item.Price);

                addItemCommand.ExecuteNonQuery();
            }
        }


        public void updateInvoice(InputModel input, QC.SqlConnection connection, int invoiceId)
        {
            DateTime paymentDueDate = CalculateDueDate(input.PaymentTerms, input.InvoiceDate);

            var updateInvoiceCommand = connection.CreateCommand();
            updateInvoiceCommand.CommandText = @"
                UPDATE Invoices
                SET Date = @Date,
                    PaymentTerms = @PaymentTerms,
                    Description = @Description,
                    PaymentDueDate = @PaymentDueDate,
                    BillFromAddress = @BillFromAddress,
                    BillFromCity = @BillFromCity,
                    BillFromPostal = @BillFromPostal,
                    BillFromCountry = @BillFromCountry
                WHERE InvoiceID = @InvoiceID;";
            updateInvoiceCommand.Parameters.AddWithValue("@InvoiceID", invoiceId);
            updateInvoiceCommand.Parameters.AddWithValue("@Date", input.InvoiceDate);
            updateInvoiceCommand.Parameters.AddWithValue("@PaymentTerms", input.PaymentTerms);
            updateInvoiceCommand.Parameters.AddWithValue("@PaymentDueDate", paymentDueDate);
            updateInvoiceCommand.Parameters.AddWithValue("@Description", input.Description);
            updateInvoiceCommand.Parameters.AddWithValue("@BillFromAddress", input.BillFromAddress);
            updateInvoiceCommand.Parameters.AddWithValue("@BillFromCity", input.BillFromCity);
            updateInvoiceCommand.Parameters.AddWithValue("@BillFromPostal", input.BillFromPostal);
            updateInvoiceCommand.Parameters.AddWithValue("@BillFromCountry", input.BillFromCountry);

            updateInvoiceCommand.ExecuteNonQuery();



        }

        public void updateClient(InputModel input, QC.SqlConnection connection, int invoiceId)
        {

            var updateClientCommand = connection.CreateCommand();
            updateClientCommand.CommandText = @"
                UPDATE Clients
                SET Name = @Name,
                    Email = @Email,
                    Address = @Address,
                    City = @City,
                    Country = @Country,
                    PostalCode = @PostalCode
                WHERE ClientID = @ClientID
                ";
            updateClientCommand.Parameters.AddWithValue("@ClientID", getClientId(connection, invoiceId));
            updateClientCommand.Parameters.AddWithValue("@Name", input.Client.ClientName);
            updateClientCommand.Parameters.AddWithValue("@Email", input.Client.ClientEmail);
            updateClientCommand.Parameters.AddWithValue("@Address", input.Client.ClientAddress);
            updateClientCommand.Parameters.AddWithValue("@City", input.Client.ClientCity);
            updateClientCommand.Parameters.AddWithValue("@Country", input.Client.ClientCountry);
            updateClientCommand.Parameters.AddWithValue("@PostalCode", input.Client.ClientPostal);

            updateClientCommand.ExecuteNonQuery();
        }


        public void updateItems(InputModel input, QC.SqlConnection connection, int invoiceId)
        {
            var deleteItemsCommand = connection.CreateCommand();
            deleteItemsCommand.CommandText = @"
                DELETE FROM Items
                WHERE InvoiceID = @InvoiceID;";
            deleteItemsCommand.Parameters.AddWithValue("@InvoiceID", invoiceId);
            deleteItemsCommand.ExecuteNonQuery();


            foreach (var item in input.Items)
            {

                var addItemCommand = connection.CreateCommand();
                addItemCommand.CommandText = @"
                    INSERT INTO Items(InvoiceID, Name, Quantity, Price)
                    VALUES(@InvoiceID, @Name, @Quantity, @Price);";

                addItemCommand.Parameters.AddWithValue("@InvoiceID", invoiceId);
                addItemCommand.Parameters.AddWithValue("@Name", item.Name);
                addItemCommand.Parameters.AddWithValue("@Quantity", item.Quantity);
                addItemCommand.Parameters.AddWithValue("@Price", item.Price);

                addItemCommand.ExecuteNonQuery();
            }
        }

        public void updateStatus(string id)
        {
            using (var connection = new QC.SqlConnection(
                 "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                 "Database = invoice;" +
                 "Trusted_Connection=True; TrustServerCertificate=True;"
                 ))
            {
                connection.Open();
                var setStatusCommand = connection.CreateCommand();
                setStatusCommand.CommandText = @"
                UPDATE Invoices
                SET Status = 'Paid'
                WHERE InvoiceId = @InvoiceId;";
                setStatusCommand.Parameters.AddWithValue("@InvoiceID", Int32.Parse(id));

                setStatusCommand.ExecuteNonQuery();

            }

        }

        public DateTime CalculateDueDate(string paymentTerms, DateTime invoiceDate)
        {
            TimeSpan ts = new TimeSpan();
            switch (paymentTerms)
            {
                case "net-1-day":
                    ts = new TimeSpan(1, 0, 0, 0);
                    break;

                case "net-7-days":
                    ts = new TimeSpan(7, 0, 0, 0);
                    break;

                case "net-15-days":
                    ts = new TimeSpan(15, 0, 0, 0);
                    break;

                case "net-30-days":
                    ts = new TimeSpan(30, 0, 0, 0);
                    break;
            }

            return invoiceDate + ts;
        }

        public void CalculateTotalFees(QC.SqlConnection connection, int id)
        {


            int totalFees = 0;

            var getItemsCommand = connection.CreateCommand();
            getItemsCommand.CommandText = @"
                    SELECT Items.Price, Items.Quantity
                    FROM Invoices
                    JOIN Items
                    ON Invoices.InvoiceID = Items.InvoiceID
                    WHERE Items.InvoiceID = @InvoiceID;";


            getItemsCommand.Parameters.AddWithValue("@InvoiceID", id);

            var reader = getItemsCommand.ExecuteReader();
            while (reader.Read())
            {
                int price = Int32.Parse(reader[0].ToString());
                int quantity = Int32.Parse(reader[1].ToString());
                int totalItemPrice = price * quantity;
                totalFees += totalItemPrice;
            }

            reader.Close();

            var updateFeesCommand = connection.CreateCommand();
            updateFeesCommand.CommandText = @"
                    UPDATE Invoices
                    SET TotalFees = @TotalFees
                    WHERE InvoiceID = @InvoiceID";

            updateFeesCommand.Parameters.AddWithValue("@InvoiceID", id);
            updateFeesCommand.Parameters.AddWithValue("@TotalFees", totalFees);
            updateFeesCommand.ExecuteNonQuery();

        }

        public int getClientId(QC.SqlConnection connection, int invoiceId)
        {
            int clientId = 0;

            var getClientIdCommand = connection.CreateCommand();
            getClientIdCommand.CommandText = @"
                SELECT ClientID
                FROM Invoices
                WHERE InvoiceID = @InvoiceID
                ";
            getClientIdCommand.Parameters.AddWithValue("@InvoiceID", invoiceId);
            var reader = getClientIdCommand.ExecuteReader();

            while (reader.Read())
            {
                clientId = Int32.Parse(reader[0].ToString());
            }

            reader.Close();

            return clientId;
        }

        public async Task<List<Client>> GetClients()

        {
            List<Client> clients = new List<Client>();


            return clients;

        }

        public async Task<DataTable> GetInvoiceInfo(string invoiceId)
        {
            DataTable result = new DataTable();

            result.Columns.Add("InvoiceId");
            result.Columns.Add("Description");
            result.Columns.Add("Date");
            result.Columns.Add("DueDate");
            result.Columns.Add("TotalFees");
            result.Columns.Add("Status");

            using (var connection = new QC.SqlConnection(
                 "Server = LAPTOP-IJL7V72O\\SQLEXPRESS;" +
                 "Database = invoice;" +
                 "Trusted_Connection=True; TrustServerCertificate=True;"
                 ))
            {

                await connection.OpenAsync();

                var getInvoiceCommand = connection.CreateCommand();
                getInvoiceCommand.CommandText = @"
                SELECT InvoiceID, Description, Date, PaymentDueDate, TotalFees, Status
                FROM Invoices
                WHERE InvoiceID = @InvoiceID";
                getInvoiceCommand.Parameters.AddWithValue("@InvoiceID", Int32.Parse(invoiceId));


                DataRow row;

                using var reader = await getInvoiceCommand.ExecuteReaderAsync();
                while (reader.Read())
                {
                    row = result.NewRow();
                    row["InvoiceId"] = reader[0].ToString();
                    row["Description"] = reader[1].ToString();
                    row["Date"] = reader[2].ToString();
                    row["DueDate"] = reader[3].ToString();
                    row["TotalFees"] = reader[4].ToString();
                    row["Status"] = reader[5].ToString();

                    result.Rows.Add(row);
                }

                reader.Close();
            }

            return result;
        }
    }

}


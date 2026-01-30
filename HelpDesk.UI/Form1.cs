using HelpDesk.BLL;
using HelpDesk.DAL;
using HelpDesk.Model;
using HelpDesk.DTO;

namespace HelpDesk.UI
{
    public partial class Form1 : Form
    {
        private readonly ITicketService _ticketService;
        private readonly ITicketCategoryRepository _ticketCategoryRepository;
        private readonly IEmployeeRepository _employeeRepository;

        public Form1(
            ITicketService ticketService,
            ITicketCategoryRepository ticketCategoryRepository,
            IEmployeeRepository employeeRepository)
        {
            InitializeComponent();
            _ticketService = ticketService;
            _ticketCategoryRepository = ticketCategoryRepository;
            _employeeRepository = employeeRepository;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            LoadDefaultValues();
            LoadTickets();
        }

        private void LoadDefaultValues()
        {
            cmbCategory.DataSource = _ticketCategoryRepository.GetAll();
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Id";

            cmbAssignedTo.DataSource = _employeeRepository.GetAll();
            cmbAssignedTo.DisplayMember = "FullName";
            cmbAssignedTo.ValueMember = "Id";

            cmbStatus.Items.AddRange(new string[] { "New", "In-Progress", "Resolved", "Closed" });
            cmbStatus.SelectedIndex = 0;
        }

        private void LoadTickets()
        {
            dgTickets.AutoGenerateColumns = true;
            dgTickets.DataSource = _ticketService.GetAll(null, null, null).ToList();
            dgTickets.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgTickets.ReadOnly = true;
            dgTickets.AllowUserToAddRows = false;
        }

        private void btnCreateTicket_Click(object sender, EventArgs e)
        {
            Model.Ticket ticket = new Model.Ticket()
            {
                IssueTitle = txtIssueTitle.Text,
                Description = txtDescription.Text,
                CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                AssignedEmployeeId = Convert.ToInt32(cmbAssignedTo.SelectedValue),
                Status = cmbStatus.Text
            };

            var result = _ticketService.Add(ticket);

            if (!result.isOk)
                MessageBox.Show(result.message);

            if (result.isOk)
            {
                MessageBox.Show(result.message);
                LoadDefaultValues();
                LoadTickets();
                return;
            }
        }

        private void btnUpdateTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.CurrentRow?.DataBoundItem is not DTO.Ticket selectedTicket)
            {
                MessageBox.Show("Please select a ticket to update.");
                return;
            }

            // Validate Issue Title
            if (string.IsNullOrWhiteSpace(txtIssueTitle.Text))
            {
                MessageBox.Show("Issue Title cannot be empty.");
                return;
            }

            // Validate Category
            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid category.");
                return;
            }

            // Validate Status
            string[] validStatuses = { "New", "In-Progress", "Resolved", "Closed" };
            if (!validStatuses.Contains(cmbStatus.Text))
            {
                MessageBox.Show("Status must be one of: New / In-Progress / Resolved / Closed");
                return;
            }

            // Prepare the updated ticket object
            Model.Ticket updatedTicket = new Model.Ticket()
            {
                Id = selectedTicket.Id, // important to know which ticket to update
                IssueTitle = txtIssueTitle.Text,
                Description = txtDescription.Text,
                CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                AssignedEmployeeId = Convert.ToInt32(cmbAssignedTo.SelectedValue),
                Status = cmbStatus.Text
            };

            // Call service to update
            var result = _ticketService.Update(updatedTicket);

            if (!result.isOk)
            {
                MessageBox.Show(result.message);
                return;
            }

            MessageBox.Show(result.message);
            LoadTickets();
        }

        private void dgTickets_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (dgTickets.CurrentRow?.DataBoundItem is DTO.Ticket ticket)
            {
                txtIssueTitle.Text = ticket.IssueTitle;
                txtDescription.Text = ticket.Description;
                
                cmbCategory.Text = ticket.Category;
                cmbAssignedTo.Text = ticket.AssignedEmployee;
                cmbStatus.Text = ticket.Status;

            }
        }
    }
}

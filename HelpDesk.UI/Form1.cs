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
            SetupFilterComboBoxes();
        }

        private void LoadDefaultValues()
        {
            cmbCategory.DataSource = _ticketCategoryRepository.GetAll();
            cmbCategory.DisplayMember = "Name";
            cmbCategory.ValueMember = "Id";

            cmbAssignedTo.DataSource = _employeeRepository.GetAll();
            cmbAssignedTo.DisplayMember = "FullName";
            cmbAssignedTo.ValueMember = "Id";

            cmbStatus.Items.Clear();
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
            if (string.IsNullOrWhiteSpace(txtIssueTitle.Text))
            {
                MessageBox.Show("Issue Title is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Description is required.");
                return;
            }

            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid category.");
                return;
            }

            if (cmbAssignedTo.SelectedValue == null || !int.TryParse(cmbAssignedTo.SelectedValue.ToString(), out _))
            {
                MessageBox.Show("Please select a valid employee to assign.");
                return;
            }

            if (string.IsNullOrWhiteSpace(cmbStatus.Text))
            {
                MessageBox.Show("Please select a status.");
                return;
            }

            Model.Ticket ticket = new Model.Ticket()
            {
                IssueTitle = txtIssueTitle.Text.Trim(),
                Description = txtDescription.Text.Trim(),
                CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                AssignedEmployeeId = Convert.ToInt32(cmbAssignedTo.SelectedValue),
                Status = cmbStatus.Text.Trim(),
                DateCreated = DateTime.Now
            };

            try
            {
                var result = _ticketService.Add(ticket);

                if (!result.isOk)
                {
                    MessageBox.Show(result.message);
                    return;
                }

                MessageBox.Show(result.message);
                LoadDefaultValues();
                LoadTickets();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding ticket: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private void btnUpdateTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.CurrentRow?.DataBoundItem is not DTO.Ticket selectedTicket)
            {
                MessageBox.Show("Please select a ticket to update.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtIssueTitle.Text))
            {
                MessageBox.Show("Issue Title cannot be empty.");
                return;
            }

            if (cmbCategory.SelectedItem == null)
            {
                MessageBox.Show("Please select a valid category.");
                return;
            }

            string[] validStatuses = { "New", "In-Progress", "Resolved", "Closed" };
            if (!validStatuses.Contains(cmbStatus.Text))
            {
                MessageBox.Show("Invalid status selected.");
                return;
            }

            bool isResolving = cmbStatus.Text == "Resolved" || cmbStatus.Text == "Closed";

            if (isResolving)
            {
                if (cmbAssignedTo.SelectedItem == null)
                {
                    MessageBox.Show("Assigned Employee is required to resolve a ticket.");
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtResolution.Text))
                {
                    MessageBox.Show("Resolution Notes are required to resolve a ticket.");
                    return;
                }
            }

            Model.Ticket updatedTicket = new Model.Ticket()
            {
                Id = selectedTicket.Id,
                IssueTitle = txtIssueTitle.Text,
                Description = txtDescription.Text,
                CategoryId = Convert.ToInt32(cmbCategory.SelectedValue),
                AssignedEmployeeId = cmbAssignedTo.SelectedItem != null
                                        ? Convert.ToInt32(cmbAssignedTo.SelectedValue)
                                        : null,
                Status = cmbStatus.Text,
                ResolutionNotes = txtResolution.Text,
                DateCreated = selectedTicket.DateCreated
            };

            if (isResolving)
            {
                updatedTicket.DateResolved = DateTime.Now;

                if (updatedTicket.DateResolved < updatedTicket.DateCreated)
                {
                    MessageBox.Show("Error: DateResolved cannot be earlier than DateCreated.");
                    return;
                }
            }
            else
            {
                updatedTicket.DateResolved = null;
            }

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

                txtResolution.Text = ticket.ResolutionNotes;
                
                

            }
        }

        private void btnDeleleteTicket_Click(object sender, EventArgs e)
        {
            if (dgTickets.CurrentRow?.DataBoundItem is not DTO.Ticket selectedTicket)
            {
                MessageBox.Show("Please select a ticket to delete.");
                return;
            }

            if (!chkConfirmDelete.Checked)
            {
                MessageBox.Show("Please check 'Confirm Delete' before deleting the ticket.");
                return;
            }

            var result = _ticketService.Delete(selectedTicket.Id);

            if (!result.isOk)
            {
                MessageBox.Show(result.message);
                LoadTickets();
                return;
            }

            MessageBox.Show(result.message);
            chkConfirmDelete.Checked = false;

            LoadTickets();

        }

        private void chkConfirmDelete_CheckedChanged(object sender, EventArgs e)
        {
            btnDeleleteTicket.Enabled = chkConfirmDelete.Checked;
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            var tickets = _ticketService.GetAll(null, null, null).ToList();
            if (!tickets.Any())
            {
                MessageBox.Show("No tickets to clear or delete.");
                return;
            }

            var confirmResult = MessageBox.Show(
                "Are you sure you want to delete ALL tickets?",
                "Confirm Delete All",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);

            if (confirmResult != DialogResult.Yes)
                return;

            foreach (var ticket in tickets)
            {
                var result = _ticketService.Delete(ticket.Id);
                if (!result.isOk)
                {
                    MessageBox.Show($"Failed to delete ticket ID {ticket.Id}: {result.message}");
                }
            }

            LoadTickets();

            MessageBox.Show("All tickets have been deleted successfully.");
        }

        private void btnApplyFilter_Click(object sender, EventArgs e)
        {
            try
            {
                int? categoryId = null;
                if (cmbFilterCategory.SelectedValue != null && cmbFilterCategory.SelectedValue.ToString() != "0")
                {
                    categoryId = Convert.ToInt32(cmbFilterCategory.SelectedValue);
                }

                string? status = null;
                string[] validStatuses = { "New", "In-Progress", "Resolved", "Closed" };
                if (cmbFilterStatus.Text != "All")
                {
                    if (!validStatuses.Contains(cmbFilterStatus.Text))
                    {
                        MessageBox.Show("Invalid status selected.");
                        return;
                    }
                    status = cmbFilterStatus.Text;
                }

                var filteredTickets = _ticketService.GetAll(status, categoryId, null);

                if (filteredTickets.Count == 0)
                {
                    MessageBox.Show("No tickets found matching the filter.");
                }
                dgTickets.DataSource = filteredTickets;
                dgTickets.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error applying filter: {ex.Message}");
            }
        }

        private void SetupFilterComboBoxes()
        {
            var categories = _ticketCategoryRepository.GetAll();
            categories.Insert(0, new Model.TicketCategory { Id = 0, Name = "All" }); 
            cmbFilterCategory.DataSource = categories;
            cmbFilterCategory.DisplayMember = "Name";
            cmbFilterCategory.ValueMember = "Id";
            cmbFilterCategory.SelectedIndex = 0;

            cmbFilterStatus.Items.Clear();
            cmbFilterStatus.Items.AddRange(new string[] { "All", "New", "In-Progress", "Resolved", "Closed" });
            cmbFilterStatus.SelectedIndex = 0;
        }

        private void btnResetFilter_Click(object sender, EventArgs e)
        {
            try
            {
                cmbFilterCategory.SelectedIndex = 0;
                cmbFilterStatus.SelectedIndex = 0;

                dgTickets.DataSource = _ticketService.GetAll(null, null, null);
                dgTickets.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error resetting filter: {ex.Message}");
            }
        }
    }
}

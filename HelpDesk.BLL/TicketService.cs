using HelpDesk.DAL;
using HelpDesk.DTO;
using HelpDesk.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HelpDesk.BLL
{
    public class TicketService : ITicketService
    {
        private readonly ITicketRepository _ticketRepository;

        public TicketService(ITicketRepository ticketRepository)
        {
            _ticketRepository = ticketRepository;
        }

        public List<DTO.Ticket> GetAll(string? status, int? categoryId, string? keyword)
        {
            return _ticketRepository
                .GetAll(status, categoryId, keyword)
                .Select(m => new DTO.Ticket
                {
                    Id = m.Id,
                    IssueTitle = m.IssueTitle,
                    Description = m.Description,
                    Category = m.Category.Name,
                    AssignedEmployee = m.AssignedEmployee?.FullName,
                    Status = m.Status,
                    ResolutionNotes = m.ResolutionNotes,
                    DateResolved = m.DateResolved,
                    DateCreated = m.DateCreated
                })
                .ToList();
        }

        public (bool isOk, string message) Add(Model.Ticket ticket)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ticket.IssueTitle))
                    return (false, "Title must not be empty.");

                if (ticket.CategoryId == null || ticket.CategoryId == 0)
                    return (false, "Category must be selected.");

                if (string.IsNullOrWhiteSpace(ticket.Status))
                    return (false, "Status must be selected.");

                ticket.DateCreated = DateTime.Now;

                // Trim resolution notes to avoid whitespace-only issues
                ticket.ResolutionNotes = ticket.ResolutionNotes?.Trim();

                switch (ticket.Status)
                {
                    case "New":
                        ticket.ResolutionNotes = null;
                        ticket.DateResolved = null;
                        break;

                    case "In-Progress":
                        ticket.DateResolved = null;
                        break;

                    case "Resolved":
                    case "Closed":
                        if (string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
                            return (false, "Resolution Notes are required.");

                        if (ticket.AssignedEmployeeId == null)
                            return (false, "Employee must be assigned.");

                        ticket.DateResolved = DateTime.Now;
                        if (ticket.DateResolved < ticket.DateCreated)
                            return (false, "Date Resolved cannot be earlier than Date Created.");
                        break;

                    default:
                        return (false, "Invalid ticket status.");
                }

                _ticketRepository.Add(ticket);
                _ticketRepository.Save();

                return (true, "Ticket added successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error adding ticket: {ex.Message}");
            }
        }

        public (bool isOk, string message) Update(Model.Ticket ticket)
        {
            try
            {
                var existingTicket = _ticketRepository.GetById(ticket.Id);
                if (existingTicket == null)
                    return (false, "Ticket not found.");

                if (string.IsNullOrWhiteSpace(ticket.IssueTitle))
                    return (false, "Title must not be empty.");

                if (ticket.CategoryId == null || ticket.CategoryId == 0)
                    return (false, "Category must be selected.");

                if (string.IsNullOrWhiteSpace(ticket.Status))
                    return (false, "Status must be selected.");

                // Trim resolution notes
                ticket.ResolutionNotes = ticket.ResolutionNotes?.Trim();

                // Update basic fields
                existingTicket.IssueTitle = ticket.IssueTitle;
                existingTicket.Description = ticket.Description;
                existingTicket.CategoryId = ticket.CategoryId;
                existingTicket.AssignedEmployeeId = ticket.AssignedEmployeeId;
                existingTicket.Status = ticket.Status;

                // Status handling
                switch (ticket.Status)
                {
                    case "New":
                        existingTicket.ResolutionNotes = null;
                        existingTicket.DateResolved = null;
                        break;

                    case "In-Progress":
                        existingTicket.DateResolved = null;
                        break;

                    case "Resolved":
                    case "Closed":
                        if (ticket.AssignedEmployeeId == null)
                            return (false, "Assigned Employee is required.");

                        if (string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
                            return (false, "Resolution Notes are required.");

                        existingTicket.ResolutionNotes = ticket.ResolutionNotes;
                        existingTicket.DateResolved = DateTime.Now;

                        if (existingTicket.DateResolved < existingTicket.DateCreated)
                            return (false, "Date Resolved cannot be earlier than Date Created.");
                        break;

                    default:
                        return (false, "Invalid ticket status.");
                }

                _ticketRepository.Save();
                return (true, "Ticket updated successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error updating ticket: {ex.Message}");
            }
        }

        public (bool isOk, string message) Delete(int ticketId)
        {
            try
            {
                var ticket = _ticketRepository.GetById(ticketId);
                if (ticket == null)
                    return (false, "Ticket not found.");

                _ticketRepository.Delete(ticket.Id);
                _ticketRepository.Save();

                return (true, "Ticket deleted successfully.");
            }
            catch (Exception ex)
            {
                return (false, $"Error deleting ticket: {ex.Message}");
            }
        }
    }
}

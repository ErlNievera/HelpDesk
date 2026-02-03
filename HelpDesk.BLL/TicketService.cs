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

        public List<DTO.Ticket> GetAll(string? status, int? category, string? keyword)
        {
            return _ticketRepository
                .GetAll(status, category, keyword)
                .Select(m => new DTO.Ticket 
                {
                    Id = m.Id,
                    IssueTitle = m.IssueTitle,
                    Description = m.Description,
                    Category = m.Category.Name,
                    AssignedEmployee = m.AssignedEmployee?.FullName,
                    Status = m.Status,
                    ResolutionNotes = m.ResolutionNotes,
                    DateResolved = m.DateResolved
                })
                .ToList();
        }

        public (bool isOk, string message) Add(Model.Ticket ticket)
        {
            try
            {
                if (string.IsNullOrEmpty(ticket.IssueTitle))
                    return (false, "Title must not be empty!");

                if (ticket.CategoryId == null || ticket.CategoryId == 0)
                    return (false, "Category must be selected!");

                if (string.IsNullOrEmpty(ticket.Status))
                    return (false, "Status must be selected!");

                ticket.DateCreated = DateTime.Now;

                if (ticket.Status == "New")
                {
                    ticket.ResolutionNotes = null;
                    ticket.DateResolved = null;
                }

                if (ticket.Status == "In-Progress")
                {
                    ticket.DateResolved = null;
                }

                if (ticket.Status == "Resolved" || ticket.Status == "Closed")
                {
                    if (string.IsNullOrEmpty(ticket.ResolutionNotes))
                        return (false, "Resolution must not be empty!");

                    if (ticket.AssignedEmployeeId == null)
                        return (false, "Employee must be selected!");

                    ticket.DateResolved = DateTime.Now;

                    if (ticket.DateResolved < ticket.DateCreated)
                        return (false, "Date Resolved cannot be earlier than Date Created!");
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

                // Update basic fields
                existingTicket.IssueTitle = ticket.IssueTitle;
                existingTicket.Description = ticket.Description;
                existingTicket.CategoryId = ticket.CategoryId;
                existingTicket.AssignedEmployeeId = ticket.AssignedEmployeeId;
                existingTicket.Status = ticket.Status;

                // Status handling
                if (ticket.Status == "New")
                {
                    existingTicket.ResolutionNotes = null;
                    existingTicket.DateResolved = null;
                }
                else if (ticket.Status == "In-Progress")
                {
                    existingTicket.DateResolved = null;
                }
                else if (ticket.Status == "Resolved" || ticket.Status == "Closed")
                {
                    // Resolve validation (AUTHORITATIVE)
                    if (ticket.AssignedEmployeeId == null)
                        return (false, "Assigned Employee is required.");

                    if (string.IsNullOrWhiteSpace(ticket.ResolutionNotes))
                        return (false, "Resolution Notes are required.");

                    existingTicket.ResolutionNotes = ticket.ResolutionNotes;
                    existingTicket.DateResolved = DateTime.Now;

                    if (existingTicket.DateResolved < existingTicket.DateCreated)
                        return (false, "Date Resolved cannot be earlier than Date Created.");
                }
                else
                {
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
                // Get all tickets and find the one to delete
                var ticket = _ticketRepository
             .GetAll(null, null, null)
             .FirstOrDefault(t => t.Id == ticketId);

                if (ticket == null)
                    return (false, "Ticket not found.");

                // Pass the Ticket object, not the ID
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

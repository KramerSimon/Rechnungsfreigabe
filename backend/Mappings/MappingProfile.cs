using AutoMapper;
using RechnungsfreigabeAPI.DTOs;
using RechnungsfreigabeAPI.Models;

namespace RechnungsfreigabeAPI.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // User mappings
        CreateMap<User, UserDto>();
        CreateMap<CreateUserDto, User>();
        CreateMap<UpdateUserDto, User>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Role mappings
        CreateMap<Role, RoleDto>();
        CreateMap<CreateRoleDto, Role>();
        CreateMap<UpdateRoleDto, Role>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Supplier mappings
        CreateMap<Supplier, SupplierDto>();
        CreateMap<CreateSupplierDto, Supplier>();

        // Cost Center mappings
        CreateMap<CostCenter, CostCenterDto>();
        CreateMap<CreateCostCenterDto, CostCenter>();

        // Project mappings
        CreateMap<Project, ProjectDto>();
        CreateMap<CreateProjectDto, Project>();

        // Invoice mappings
        CreateMap<Invoice, InvoiceDto>()
            .ForMember(dest => dest.IsOverdue, opt => opt.MapFrom(src => 
                src.DueDate < DateTime.UtcNow && 
                src.Status != InvoiceStatus.Bezahlt && 
                src.Status != InvoiceStatus.Storniert))
            .ForMember(dest => dest.DaysOverdue, opt => opt.MapFrom(src => 
                src.DueDate < DateTime.UtcNow ? (DateTime.UtcNow - src.DueDate).Days : 0));
        CreateMap<CreateInvoiceDto, Invoice>();
        CreateMap<UpdateInvoiceDto, Invoice>()
            .ForAllMembers(opt => opt.Condition((src, dest, srcMember) => srcMember != null));

        // Approval Workflow mappings
        CreateMap<ApprovalWorkflow, ApprovalWorkflowDto>();

        // Invoice History mappings  
        CreateMap<InvoiceHistory, InvoiceHistoryDto>()
            .ForMember(dest => dest.ActionType, opt => opt.MapFrom(src => src.ActionType.ToString()))
            .ForMember(dest => dest.ActionSource, opt => opt.MapFrom(src => src.ActionSource.ToString()))
            .ForMember(dest => dest.FieldChanges, opt => opt.MapFrom(src => ParseFieldChanges(src.FieldChanges)))
            .ForMember(dest => dest.DisplayIcon, opt => opt.MapFrom(src => GetDisplayIcon(src.ActionType, src.ActionSource)))
            .ForMember(dest => dest.DisplayColor, opt => opt.MapFrom(src => GetDisplayColor(src.ActionType, src.ActionSource)));

        // Notification mappings
        CreateMap<Notification, NotificationDto>();
    }

    private static string GetDisplayIcon(HistoryActionType actionType, HistoryActionSource actionSource)
    {
        return actionType switch
        {
            HistoryActionType.Created => "+",
            HistoryActionType.Approved => "✓",
            HistoryActionType.Rejected => "✗",
            HistoryActionType.Escalated => "!",
            HistoryActionType.Assigned => "@", 
            HistoryActionType.DataCompleted => "✎",
            HistoryActionType.PaymentInitiated => "€",
            HistoryActionType.PolicyTriggered => "⚙",
            HistoryActionType.SystemAction => "🤖",
            _ => "•"
        };
    }

    private static string GetDisplayColor(HistoryActionType actionType, HistoryActionSource actionSource)
    {
        return actionSource switch
        {
            HistoryActionSource.System => "#6c757d", // gray
            HistoryActionSource.Escalation => "#dc3545", // red
            _ => actionType switch
            {
                HistoryActionType.Approved => "#28a745", // green
                HistoryActionType.Rejected => "#dc3545", // red
                HistoryActionType.PaymentInitiated => "#007bff", // blue
                _ => "#495057" // dark gray
            }
        };
    }

    private static Dictionary<string, object>? ParseFieldChanges(string? fieldChanges)
    {
        if (string.IsNullOrEmpty(fieldChanges))
            return null;
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fieldChanges);
        }
        catch
        {
            return null;
        }
    }
}
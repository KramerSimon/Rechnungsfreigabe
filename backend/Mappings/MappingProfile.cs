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

        // Notification mappings
        CreateMap<Notification, NotificationDto>();
    }
}
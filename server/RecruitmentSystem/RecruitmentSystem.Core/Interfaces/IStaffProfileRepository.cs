using RecruitmentSystem.Core.Entities;

namespace RecruitmentSystem.Core.Interfaces
{
    public interface IStaffProfileRepository
    {
        Task<StaffProfile?> GetByIdAsync(Guid id);
        Task<StaffProfile?> GetByUserIdAsync(Guid userId);
        Task<StaffProfile?> GetByEmployeeCodeAsync(string employeeCode);
        Task<StaffProfile> CreateAsync(StaffProfile profile);
        Task<StaffProfile> UpdateAsync(StaffProfile profile);
        Task<bool> DeleteAsync(Guid id);
        Task<bool> ExistsAsync(Guid id);
        Task<bool> ExistsByUserIdAsync(Guid userId);
        Task<IEnumerable<StaffProfile>> GetByDepartmentAsync(string department);
        Task<IEnumerable<StaffProfile>> GetActiveStaffAsync();
        Task<(List<StaffProfile> Items, int TotalCount)> SearchStaffAsync(
            string? query,
            string? department,
            string? location,
            IEnumerable<string>? roles,
            string? status,
            int pageNumber,
            int pageSize);
    }
}
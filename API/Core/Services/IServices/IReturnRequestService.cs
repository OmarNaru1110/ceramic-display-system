using Core.DTOs.ReturnRequest;
using CORE.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Services.IServices
{
    public interface IReturnRequestService
    {
        Task<ResponseDto<GetReturnRequestDto>> CreateReturnRequestAsync(CreateReturnRequestDto dto, int? sellerId);
        Task<ResponseDto<GetReturnRequestDto>> GetReturnRequestByIdAsync(int id, int? userId, string[]? roles);
        Task<ResponseDto<PagedResultDto<GetReturnRequestDto>>> GetReturnRequestsAsync(ReturnRequestFilterDto filter, int? userId, string[]? roles);
        Task<ResponseDto<GetReturnRequestDto>> AcceptReturnRequestAsync(int id, int? adminId, string? adminNotes);
        Task<ResponseDto<GetReturnRequestDto>> RejectReturnRequestAsync(int id, int? adminId, string? adminNotes);
    }
}

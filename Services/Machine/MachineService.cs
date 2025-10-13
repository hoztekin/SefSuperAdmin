using App.Repositories;
using App.Repositories.Machines;
using App.Services.Machine.Dtos;
using App.Services.Machine.Validation;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace App.Services.Machine
{
    public class MachineService(IMachineRepository machineRepository,
                                IUnitOfWork unitOfWork,
                                IMapper mapper,
                                ILogger<MachineService> logger,
                                IHttpClientFactory httpClientFactory) : IMachineService
    {
        public async Task<ServiceResult<List<MachineListDto>>> GetAllListAsync()
        {
            try
            {
                var machines = await machineRepository
                    .Where(x => !x.IsDeleted)
                    .OrderBy(x => x.BranchName)
                    .ToListAsync();

                var machineListDtos = mapper.Map<List<MachineListDto>>(machines);

                return ServiceResult<List<MachineListDto>>.Success(machineListDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makineler listelenirken hata oluştu");
                return ServiceResult<List<MachineListDto>>.Fail("Makineler yüklenirken hata oluştu", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<List<MachineListDto>>> GetActiveListAsync()
        {
            try
            {
                var machines = await machineRepository
                    .Where(x => !x.IsDeleted && x.IsActive)
                    .OrderBy(x => x.BranchName)
                    .ToListAsync();

                var machineListDtos = mapper.Map<List<MachineListDto>>(machines);

                return ServiceResult<List<MachineListDto>>.Success(machineListDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Aktif makineler listelenirken hata oluştu");
                return ServiceResult<List<MachineListDto>>.Fail("Aktif makineler yüklenirken hata oluştu", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<MachineDto>> GetByIdAsync(int id)
        {
            try
            {
                var machine = await machineRepository.GetByIdAsync(id);

                if (machine == null || machine.IsDeleted)
                {
                    return ServiceResult<MachineDto>.Fail("Makine bulunamadı", System.Net.HttpStatusCode.NotFound);
                }

                var machineDto = mapper.Map<MachineDto>(machine);

                return ServiceResult<MachineDto>.Success(machineDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makine getirilirken hata oluştu. ID: {Id}", id);
                return ServiceResult<MachineDto>.Fail("Makine bilgisi getirilemedi", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<MachineDto>> GetByCodeAsync(string code)
        {
            try
            {
                var machine = await machineRepository
                    .Where(x => x.Code == code && !x.IsDeleted)
                    .FirstOrDefaultAsync();

                if (machine == null)
                {
                    return ServiceResult<MachineDto>.Fail("Makine bulunamadı", System.Net.HttpStatusCode.NotFound);
                }

                var machineDto = mapper.Map<MachineDto>(machine);

                return ServiceResult<MachineDto>.Success(machineDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makine kodu ile makine getirilirken hata oluştu. Code: {Code}", code);
                return ServiceResult<MachineDto>.Fail("Makine bilgisi getirilemedi", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<List<MachineListDto>>> GetByBranchIdAsync(string branchId)
        {
            try
            {
                var machines = await machineRepository
                    .Where(x => x.BranchId == branchId && !x.IsDeleted)
                    .OrderBy(x => x.Code)
                    .ToListAsync();

                var machineListDtos = mapper.Map<List<MachineListDto>>(machines);

                return ServiceResult<List<MachineListDto>>.Success(machineListDtos);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Şube ID'sine göre makineler listelenirken hata oluştu. BranchId: {BranchId}", branchId);
                return ServiceResult<List<MachineListDto>>.Fail("Şubeye ait makineler yüklenirken hata oluştu", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<CreateMachineDto>> CreateAsync(CreateMachineDto createMachineDto)
        {
            try
            {
                // Kod benzersizlik kontrolü
                var codeExists = await IsCodeExistsAsync(createMachineDto.Code);
                if (codeExists.Data)
                {
                    return ServiceResult<CreateMachineDto>.Fail("Bu kod zaten kullanımda", System.Net.HttpStatusCode.BadRequest);
                }

                // BranchId benzersizlik kontrolü
                var branchIdExists = await IsBranchIdExistsAsync(createMachineDto.BranchId);
                if (branchIdExists.Data)
                {
                    return ServiceResult<CreateMachineDto>.Fail("Bu şube ID'si zaten kullanımda", System.Net.HttpStatusCode.BadRequest);
                }

                var newMachine = mapper.Map<Repositories.Machines.Machine>(createMachineDto);

                await machineRepository.AddAsync(newMachine);
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Yeni makine oluşturuldu. Code: {Code}, BranchName: {BranchName}",
                    createMachineDto.Code, createMachineDto.BranchName);

                return ServiceResult<CreateMachineDto>.Success(createMachineDto);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makine oluşturulurken hata oluştu. Code: {Code}", createMachineDto.Code);
                return ServiceResult<CreateMachineDto>.Fail("Makine oluşturulamadı", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> UpdateAsync(UpdateMachineDto updateMachineDto)
        {
            try
            {
                var existingMachine = await machineRepository.GetByIdAsync(updateMachineDto.Id);

                if (existingMachine == null || existingMachine.IsDeleted)
                {
                    return ServiceResult.Fail("Güncellenecek makine bulunamadı", System.Net.HttpStatusCode.NotFound);
                }

                // Kod benzersizlik kontrolü (mevcut makine hariç)
                var codeExists = await IsCodeExistsAsync(updateMachineDto.Code, updateMachineDto.Id);
                if (codeExists.Data)
                {
                    return ServiceResult.Fail("Bu kod başka bir makine tarafından kullanılıyor", System.Net.HttpStatusCode.BadRequest);
                }

                // BranchId benzersizlik kontrolü (mevcut makine hariç)
                var branchIdExists = await IsBranchIdExistsAsync(updateMachineDto.BranchId, updateMachineDto.Id);
                if (branchIdExists.Data)
                {
                    return ServiceResult.Fail("Bu şube ID'si başka bir makine tarafından kullanılıyor", System.Net.HttpStatusCode.BadRequest);
                }

                // Güncelleme
                mapper.Map(updateMachineDto, existingMachine);

                machineRepository.Update(existingMachine);
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Makine güncellendi. ID: {Id}, Code: {Code}",
                    updateMachineDto.Id, updateMachineDto.Code);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makine güncellenirken hata oluştu. ID: {Id}", updateMachineDto.Id);
                return ServiceResult.Fail("Makine güncellenemedi", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> DeleteAsync(int id)
        {
            try
            {
                var machine = await machineRepository.GetByIdAsync(id);

                if (machine == null || machine.IsDeleted)
                {
                    return ServiceResult.Fail("Silinecek makine bulunamadı", System.Net.HttpStatusCode.NotFound);
                }

                // Soft delete
                machine.IsDeleted = true;
                machine.IsActive = false;

                machineRepository.Update(machine);
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Makine silindi. ID: {Id}, Code: {Code}", id, machine.Code);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makine silinirken hata oluştu. ID: {Id}", id);
                return ServiceResult.Fail("Makine silinemedi", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult> SetActiveStatusAsync(int id, bool isActive)
        {
            try
            {
                var machine = await machineRepository.GetByIdAsync(id);

                if (machine == null || machine.IsDeleted)
                {
                    return ServiceResult.Fail("Makine bulunamadı", System.Net.HttpStatusCode.NotFound);
                }

                machine.IsActive = isActive;

                machineRepository.Update(machine);
                await unitOfWork.SaveChangesAsync();

                logger.LogInformation("Makine durumu güncellendi. ID: {Id}, IsActive: {IsActive}", id, isActive);

                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Makine durumu güncellenirken hata oluştu. ID: {Id}", id);
                return ServiceResult.Fail("Makine durumu güncellenemedi", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<bool>> IsCodeExistsAsync(string code, int? excludeId = null)
        {
            try
            {
                var query = machineRepository.Where(x => x.Code == code && !x.IsDeleted);

                if (excludeId.HasValue)
                {
                    query = query.Where(x => x.Id != excludeId.Value);
                }

                var exists = await query.AnyAsync();

                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Kod kontrolü yapılırken hata oluştu. Code: {Code}", code);
                return ServiceResult<bool>.Fail("Kod kontrolü yapılamadı", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<bool>> IsBranchIdExistsAsync(string branchId, int? excludeId = null)
        {
            try
            {
                var query = machineRepository.Where(x => x.BranchId == branchId && !x.IsDeleted);

                if (excludeId.HasValue)
                {
                    query = query.Where(x => x.Id != excludeId.Value);
                }

                var exists = await query.AnyAsync();

                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Şube ID kontrolü yapılırken hata oluştu. BranchId: {BranchId}", branchId);
                return ServiceResult<bool>.Fail("Şube ID kontrolü yapılamadı", System.Net.HttpStatusCode.InternalServerError);
            }
        }

        public async Task<ServiceResult<bool>> CheckApiConnectionAsync(string apiAddress)
        {
            try
            {
                using var httpClient = httpClientFactory.CreateClient();
                httpClient.Timeout = TimeSpan.FromSeconds(10);

                var healthUrl = $"{apiAddress.TrimEnd('/')}/health";
                var response = await httpClient.GetAsync(healthUrl);
                var isConnected = response.IsSuccessStatusCode;

                return ServiceResult<bool>.Success(isConnected);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "API bağlantısı kontrol edilirken hata oluştu. ApiAddress: {ApiAddress}", apiAddress);
                return ServiceResult<bool>.Success(false);
            }
        }
    }
}

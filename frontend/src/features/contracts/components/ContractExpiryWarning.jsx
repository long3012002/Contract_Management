import { AlertTriangle, ChevronRight, CalendarDays } from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { Popover, PopoverContent, PopoverTrigger } from '@/components/ui/popover';
import { formatVND } from '@/utils/formatters';

export default function ContractExpiryWarning({ expiringContracts = [], expiringPaymentPlans = [] }) {
  const navigate = useNavigate();
  const totalWarnings = expiringContracts.length + expiringPaymentPlans.length;

  if (totalWarnings === 0) return null;

  return (
    <div className="flex items-center gap-2">
      {/* 1. Cảnh báo Hợp đồng */}
      {expiringContracts.length > 0 && (
        <Popover>
          <PopoverTrigger asChild>
            <button className="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full bg-amber-500/10 text-amber-700 border border-amber-500/20 hover:bg-amber-500/20 transition-all cursor-pointer shadow-sm active:scale-95 duration-150 shrink-0">
              <AlertTriangle className="size-3.5 text-amber-600" />
              <span>{expiringContracts.length} HĐ hết hạn</span>
            </button>
          </PopoverTrigger>
          <PopoverContent align="start" className="w-85 p-3 bg-white/98 dark:bg-zinc-900/98 border border-amber-200/80 dark:border-amber-900/40 shadow-xl rounded-xl backdrop-blur-sm">
            <div className="space-y-4">
              <div className="flex items-center gap-2 border-b pb-2 border-amber-200/50 dark:border-amber-900/50">
                <AlertTriangle className="size-4 text-amber-600" />
                <span className="font-semibold text-sm text-foreground">Cảnh báo hệ thống</span>
              </div>
              <div className="space-y-2">
                <div className="text-[10px] font-bold uppercase text-amber-800 dark:text-amber-600/90 tracking-wider">Hợp đồng sắp hết hạn (≤ 30 ngày)</div>
                <div className="space-y-1.5 max-h-[300px] overflow-y-auto pr-1">
                  {expiringContracts.map(contract => {
                    const expDate = new Date(contract.expiredDate);
                    const today = new Date();
                    today.setHours(0, 0, 0, 0);
                    const diffDays = Math.ceil((expDate - today) / (1000 * 60 * 60 * 24));
                    const isPast = diffDays < 0;

                    return (
                      <div key={contract.id} className="flex items-center justify-between gap-2 p-2 rounded-lg bg-amber-500/5 hover:bg-amber-500/10 transition-colors text-xs">
                        <div className="min-w-0 flex-1">
                          <div className="font-semibold text-amber-950 dark:text-amber-100 truncate">{contract.code}</div>
                          <div className="text-muted-foreground truncate" title={contract.name}>{contract.name}</div>
                        </div>
                        <div className="flex items-center gap-1.5 shrink-0">
                          <span className="font-medium text-[10px] bg-amber-500/20 text-amber-800 px-2 py-0.5 rounded-full whitespace-nowrap">
                            {isPast ? `Quá hạn ${Math.abs(diffDays)} ngày` : `Còn ${diffDays} ngày`}
                          </span>
                          <button 
                            onClick={() => navigate(`/contracts/${contract.id}`)}
                            className="p-1 hover:bg-amber-500/20 rounded-md transition-colors text-amber-700 cursor-pointer"
                            title="Xem chi tiết"
                          >
                            <ChevronRight className="size-3.5" />
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          </PopoverContent>
        </Popover>
      )}

      {/* 2. Cảnh báo Thanh toán */}
      {expiringPaymentPlans.length > 0 && (
        <Popover>
          <PopoverTrigger asChild>
            <button className="inline-flex items-center gap-1.5 text-xs font-semibold px-2.5 py-1 rounded-full bg-rose-500/10 text-rose-700 border border-rose-500/20 hover:bg-rose-500/20 transition-all cursor-pointer shadow-sm active:scale-95 duration-150 shrink-0">
              <CalendarDays className="size-3.5 text-rose-600" />
              <span>{expiringPaymentPlans.length} TT đến hạn</span>
            </button>
          </PopoverTrigger>
          <PopoverContent align="start" className="w-85 p-3 bg-white/98 dark:bg-zinc-900/98 border border-rose-200/80 dark:border-rose-900/40 shadow-xl rounded-xl backdrop-blur-sm">
            <div className="space-y-4">
              <div className="flex items-center gap-2 border-b pb-2 border-rose-200/50 dark:border-rose-900/50">
                <CalendarDays className="size-4 text-rose-600" />
                <span className="font-semibold text-sm text-foreground">Cảnh báo thanh toán</span>
              </div>
              <div className="space-y-2">
                <div className="text-[10px] font-bold uppercase text-rose-800 dark:text-rose-600/90 tracking-wider">Đợt TT đến hạn (≤ 15 ngày)</div>
                <div className="space-y-1.5 max-h-[300px] overflow-y-auto pr-1">
                  {expiringPaymentPlans.map(plan => {
                    return (
                      <div key={plan.id} className="flex items-center justify-between gap-2 p-2 rounded-lg bg-rose-500/5 hover:bg-rose-500/10 transition-colors text-xs">
                        <div className="min-w-0 flex-1">
                          <div className="font-semibold text-rose-950 dark:text-rose-100 truncate">{plan.name}</div>
                          <div className="text-muted-foreground text-[11px] truncate">{plan.contractCode} • {formatVND(plan.value)}</div>
                        </div>
                        <div className="flex items-center gap-1.5 shrink-0">
                          <span className="font-medium text-[10px] bg-rose-500/20 text-rose-800 px-2 py-0.5 rounded-full whitespace-nowrap">
                            {plan.isPast ? `Quá hạn ${Math.abs(plan.diffDays)} ngày` : `Còn ${plan.diffDays} ngày`}
                          </span>
                          <button 
                            onClick={() => navigate(`/contracts/${plan.contractId}`)}
                            className="p-1 hover:bg-rose-500/20 rounded-md transition-colors text-rose-700 cursor-pointer"
                            title="Xem chi tiết hợp đồng"
                          >
                            <ChevronRight className="size-3.5" />
                          </button>
                        </div>
                      </div>
                    );
                  })}
                </div>
              </div>
            </div>
          </PopoverContent>
        </Popover>
      )}
    </div>
  );
}




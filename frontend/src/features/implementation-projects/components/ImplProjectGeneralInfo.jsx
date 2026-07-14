import { formatVND, formatDate } from '@/utils/formatters';

export default function ImplProjectGeneralInfo({ project }) {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Cột trái: Thông tin */}
      <div className="lg:col-span-2 space-y-6">
        <div className="bg-card border border-border rounded-custom p-5 shadow-sm space-y-4">
          <h3 className="text-base font-semibold border-b border-border pb-2">Chi tiết dự án</h3>
          <div className="grid grid-cols-2 gap-x-8 gap-y-4 text-sm">
            <div className="space-y-1">
              <p className="text-muted-foreground">Đơn vị chủ trì</p>
              <p className="font-medium">{project.unit}</p>
            </div>
            <div className="space-y-1">
              <p className="text-muted-foreground">Chủ đầu tư</p>
              <p className="font-medium">{project.investor}</p>
            </div>
            <div className="space-y-1">
              <p className="text-muted-foreground">Ngày bắt đầu</p>
              <p className="font-medium">{formatDate(project.startDate)}</p>
            </div>
            <div className="space-y-1">
              <p className="text-muted-foreground">Ngày kết thúc (dự kiến)</p>
              <p className="font-medium">{formatDate(project.endDate)}</p>
            </div>
            <div className="col-span-2 space-y-1 pt-2">
              <p className="text-muted-foreground">Mô tả</p>
              <p className="text-foreground/90 whitespace-pre-wrap">{project.description || 'Không có mô tả.'}</p>
            </div>
          </div>
        </div>
      </div>

      {/* Cột phải: Ngân sách & Nguồn vốn */}
      <div className="space-y-6">
        <div className="bg-primary/5 border border-primary/20 rounded-custom p-5 shadow-sm">
          <h3 className="text-sm font-medium text-primary mb-1">Tổng mức đầu tư</h3>
          <p className="text-2xl font-bold text-primary tabular-nums tracking-tight">
            {formatVND(project.totalBudget)}
          </p>
        </div>

        <div className="bg-card border border-border rounded-custom p-5 shadow-sm space-y-4">
          <h3 className="text-base font-semibold border-b border-border pb-2 flex items-center justify-between">
            Nguồn vốn thành phần
            <span className="text-xs font-normal text-muted-foreground bg-muted px-2 py-0.5 rounded-full">
              {project.sourceProjects.length} nguồn
            </span>
          </h3>
          
          {project.sourceProjects.length > 0 ? (
            <div className="space-y-3">
              {project.sourceProjects.map((sp) => (
                <div key={sp.id} className="p-3 bg-secondary/50 rounded-md border border-border">
                  <p className="font-medium text-sm truncate" title={sp.name}>{sp.name}</p>
                  <div className="flex items-center justify-between mt-1 text-xs text-muted-foreground">
                    <span className="font-mono">{sp.code}</span>
                    <span className="font-semibold tabular-nums text-foreground">{formatVND(sp.value)}</span>
                  </div>
                </div>
              ))}
            </div>
          ) : (
            <p className="text-sm text-muted-foreground">Chưa liên kết nguồn vốn nào.</p>
          )}
        </div>
      </div>
    </div>
  );
}

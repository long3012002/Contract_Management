import sendMail from "../sendMail";

export default function Dashboard() {
  return (
    <div className="space-y-6">
      <div className="flex flex-col gap-2">
        <h1 className="text-3xl font-bold tracking-tight text-foreground">Dashboard</h1>
        <p className="text-muted-foreground">Tổng quan tình hình các dự án và công việc.</p>
      </div>
      <button onClick={sendMail}>Click sendmail</button>
      <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-4">
        {/* Mock Cards */}
        {[1, 2, 3, 4].map((i) => (
          <div key={i} className="p-6 bg-white border border-border rounded-xl shadow-sm">
            <h3 className="font-semibold text-sm text-muted-foreground">Thống kê {i}</h3>
            <p className="text-2xl font-bold mt-2">1,234</p>
          </div>
        ))}
      </div>
    </div>
  );
}

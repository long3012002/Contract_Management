import { useState, useEffect } from 'react';
import {
  Pagination,
  PaginationContent,
  PaginationEllipsis,
  PaginationItem,
  PaginationLink,
  PaginationNext,
  PaginationPrevious,
} from '@/components/ui/pagination';

export default function PaginationWithInput({
  totalItems = 0,
  totalPages = 1,
  currentPage = 1,
  onPageChange,
  itemsPerPage = 8,
}) {
  const [pageInput, setPageInput] = useState('1');

  useEffect(() => {
    setPageInput(currentPage.toString());
  }, [currentPage]);

  const handlePageInputChange = (e) => {
    const val = e.target.value;
    setPageInput(val);
    const num = parseInt(val, 10);
    if (!isNaN(num) && num >= 1 && num <= totalPages) {
      onPageChange(num);
    }
  };

  if (totalItems === 0) return null;

  return (
    <div className="flex flex-col sm:flex-row items-center justify-between gap-4 px-2 py-4 border-t border-border mt-4">
      <p className="text-sm text-muted-foreground tabular-nums">
        Hiển thị {(currentPage - 1) * itemsPerPage + 1} –{' '}
        {Math.min(currentPage * itemsPerPage, totalItems)} trong số {totalItems} kết quả
      </p>
      
      {totalPages > 1 && (
        <div className="flex items-center gap-4">
          <Pagination className="w-auto mx-0">
            <PaginationContent>
              <PaginationItem>
                <PaginationPrevious
                  onClick={() => onPageChange(Math.max(currentPage - 1, 1))}
                  disabled={currentPage === 1}
                  className={`cursor-pointer ${currentPage === 1 ? 'pointer-events-none opacity-50' : ''}`}
                />
              </PaginationItem>

              <PaginationItem>
                <PaginationLink isActive className="pointer-events-none">
                  {currentPage}
                </PaginationLink>
              </PaginationItem>

              {currentPage < totalPages && (
                <PaginationItem>
                  <PaginationLink
                    className="cursor-pointer"
                    onClick={() => onPageChange(currentPage + 1)}
                  >
                    {currentPage + 1}
                  </PaginationLink>
                </PaginationItem>
              )}

              {currentPage < totalPages - 1 && (
                <PaginationItem>
                  <PaginationEllipsis />
                </PaginationItem>
              )}

              <PaginationItem>
                <PaginationNext
                  onClick={() =>
                    onPageChange(Math.min(currentPage + 1, totalPages))
                  }
                  disabled={currentPage === totalPages}
                  className={`cursor-pointer ${currentPage === totalPages ? 'pointer-events-none opacity-50' : ''}`}
                />
              </PaginationItem>
            </PaginationContent>
          </Pagination>
          <div className="flex items-center gap-1.5 text-xs md:text-sm text-muted-foreground">
            <span className="whitespace-nowrap">Đến trang:</span>
            <input
              type="number"
              min={1}
              max={totalPages}
              value={pageInput}
              onChange={handlePageInputChange}
              className="w-12 text-center bg-background border border-input rounded-md py-1 text-xs md:text-sm focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:border-ring [appearance:textfield] [&::-webkit-outer-spin-button]:appearance-none [&::-webkit-inner-spin-button]:appearance-none"
            />
            <span className="whitespace-nowrap">/ {totalPages}</span>
          </div>
        </div>
      )}
    </div>
  );
}

import React, { createContext, useContext, useState, useCallback } from 'react';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from "@/components/ui/alert-dialog";

const ConfirmDialogContext = createContext();

export const useConfirm = () => {
  const context = useContext(ConfirmDialogContext);
  if (!context) {
    throw new Error("useConfirm must be used within a ConfirmDialogProvider");
  }
  return context;
};

export const ConfirmDialogProvider = ({ children }) => {
  const [open, setOpen] = useState(false);
  const [config, setConfig] = useState({});
  const [resolver, setResolver] = useState(null);

  const confirm = useCallback((options) => {
    setConfig({
      title: options.title || "Xác nhận",
      description: options.description || "Bạn có chắc chắn muốn thực hiện hành động này không?",
      confirmText: options.confirmText || "Xác nhận",
      cancelText: options.cancelText || "Hủy",
      variant: options.variant || "default", // "default" | "destructive"
    });
    setOpen(true);
    return new Promise((resolve) => {
      setResolver(() => resolve);
    });
  }, []);

  const handleConfirm = () => {
    setOpen(false);
    if (resolver) resolver(true);
  };

  const handleCancel = () => {
    setOpen(false);
    if (resolver) resolver(false);
  };

  return (
    <ConfirmDialogContext.Provider value={{ confirm }}>
      {children}
      <AlertDialog open={open} onOpenChange={(isOpen) => {
        if (!isOpen) handleCancel();
      }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>{config.title}</AlertDialogTitle>
            <AlertDialogDescription>
              {config.description}
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel onClick={handleCancel}>
              {config.cancelText}
            </AlertDialogCancel>
            <AlertDialogAction 
              onClick={handleConfirm}
              className={config.variant === "destructive" ? "bg-destructive text-destructive-foreground hover:bg-destructive/90" : ""}
            >
              {config.confirmText}
            </AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </ConfirmDialogContext.Provider>
  );
};

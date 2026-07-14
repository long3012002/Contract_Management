import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import { Toaster } from 'sonner'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from './api/queryClient'
import { BrowserRouter } from 'react-router-dom'
import { ConfirmDialogProvider } from '@/components/providers/confirm-dialog-provider'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <BrowserRouter>
        <ConfirmDialogProvider>
          <App />
        </ConfirmDialogProvider>
      </BrowserRouter>
      <Toaster position='top-right' richColors />
    </QueryClientProvider>
  </StrictMode>,
)

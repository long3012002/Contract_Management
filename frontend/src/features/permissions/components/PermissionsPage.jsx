import React, { useState } from 'react';
import { usePermissions } from '../hooks/usePermissions';
import { PermissionsProvider } from '../context/PermissionsContext';
import PermissionMatrix from './PermissionMatrix';
import PermissionHeader from './PermissionHeader';
import PermissionTabSwitcher from './PermissionTabSwitcher';
import PermissionEntityList from './PermissionEntityList';
import { Shield } from 'lucide-react';
import { useConfirm } from '@/components/providers/confirm-dialog-provider';
import EntityCrudModal from './EntityCrudModal';

function PermissionsPageContent() {
  const {
    activeTab,
    setActiveTab,
    selectedEntityId,
    setSelectedEntityId,
    departmentsQuery,
    positionsQuery,
    saveMutation,
    handleSave,
    
    // CRUD Mutations
    createDepartmentMutation,
    updateDepartmentMutation,
    deleteDepartmentMutation,
    createPositionMutation,
    updatePositionMutation,
    deletePositionMutation,
  } = usePermissions();

  const departments = departmentsQuery.data || [];
  const positions = positionsQuery.data || [];
  const isSaving = saveMutation.isPending;

  // Derive polymorphic CRUD operations and states locally
  const createMutation = activeTab === 'department' ? createDepartmentMutation : createPositionMutation;
  const updateMutation = activeTab === 'department' ? updateDepartmentMutation : updatePositionMutation;
  const deleteMutation = activeTab === 'department' ? deleteDepartmentMutation : deletePositionMutation;

  const createEntity = createMutation.mutateAsync;
  const updateEntity = updateMutation.mutateAsync;
  const deleteEntity = deleteMutation.mutateAsync;
  const isCrudLoading = createMutation.isPending || updateMutation.isPending || deleteMutation.isPending;

  const { confirm } = useConfirm();
  const [modalState, setModalState] = useState({
    isOpen: false,
    mode: 'create', // 'create' | 'edit'
    entity: null, // the department/position object being edited
  });

  const currentList = activeTab === 'department' ? departments : positions;

  const handleOpenAddModal = () => {
    setModalState({
      isOpen: true,
      mode: 'create',
      entity: null,
    });
  };

  const handleOpenEditModal = (entity) => {
    setModalState({
      isOpen: true,
      mode: 'edit',
      entity,
    });
  };

  const handleSaveModal = async (name) => {
    try {
      if (modalState.mode === 'create') {
        await createEntity(name);
      } else {
        const id = modalState.entity.id;
        await updateEntity({ id, name });
      }
      setModalState(prev => ({ ...prev, isOpen: false }));
    } catch (err) {
      console.error(err);
    }
  };

  const handleDelete = async (entity) => {
    const name = entity.tenPhongBan || entity.tenChucVu || entity.name;
    const typeText = activeTab === 'department' ? 'phòng ban' : 'chức vụ';
    const isConfirmed = await confirm({
      title: `Xóa ${typeText}`,
      description: (
        <span>
          Bạn có chắc chắn muốn xóa {typeText}{' '}
          <strong className="text-foreground font-semibold">"{name}"</strong>?
          <br />
          <span className="text-xs text-muted-foreground mt-1 inline-block">
            Hành động này không thể hoàn tác.
          </span>
        </span>
      ),
      confirmText: 'Xóa',
      cancelText: 'Hủy',
      variant: 'destructive',
    });

    if (isConfirmed) {
      try {
        await deleteEntity(entity.id);
      } catch (err) {
        console.error(err);
      }
    }
  };

  return (
    <div className="flex flex-col h-full space-y-6 animate-in fade-in slide-in-from-bottom-4 duration-500 pb-10">
      
      {/* Header Area */}
      <PermissionHeader isSaving={isSaving} onSave={handleSave} />

      {/* Main Content Area */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-6">
        
        {/* Left Sidebar / Selectors */}
        <div className="md:col-span-1 space-y-6">
          
          {/* Tab Switcher */}
          <PermissionTabSwitcher activeTab={activeTab} onTabChange={setActiveTab} />

          {/* Entity List */}
          <PermissionEntityList
            activeTab={activeTab}
            entities={currentList}
            selectedEntityId={selectedEntityId}
            onSelectEntity={setSelectedEntityId}
            onAddClick={handleOpenAddModal}
            onEditClick={handleOpenEditModal}
            onDeleteClick={handleDelete}
          />
        </div>

        {/* Right Content / Matrix */}
        <div className="md:col-span-3">
          {selectedEntityId ? (
            <div className="space-y-4">
              <div className="bg-primary/5 border border-primary/20 p-4 rounded-custom flex items-center justify-between">
                <div>
                  <h2 className="font-semibold text-primary">Thiết lập quyền</h2>
                  <p className="text-sm text-muted-foreground">
                    Cấu hình quyền truy cập cho {activeTab === 'department' ? 'phòng ban' : 'chức vụ'}: 
                    <span className="font-medium text-foreground ml-1">
                      {(() => {
                        const entity = currentList.find(e => e.id === selectedEntityId);
                        return entity ? (entity.tenPhongBan || entity.tenChucVu || entity.name) : '';
                      })()}
                    </span>
                  </p>
                </div>
              </div>
              
              <PermissionMatrix />
            </div>
          ) : (
            <div className="h-full min-h-[400px] border border-dashed border-border rounded-custom flex flex-col items-center justify-center text-muted-foreground bg-card/50">
              <Shield className="w-12 h-12 mb-4 opacity-20" />
              <p>Vui lòng chọn một {activeTab === 'department' ? 'phòng ban' : 'chức vụ'} để thiết lập quyền.</p>
            </div>
          )}
        </div>
        
      </div>

      {/* CRUD Modal */}
      <EntityCrudModal
        isOpen={modalState.isOpen}
        onClose={() => setModalState(prev => ({ ...prev, isOpen: false }))}
        title={
          modalState.mode === 'create'
            ? activeTab === 'department' ? 'Thêm phòng ban mới' : 'Thêm chức vụ mới'
            : activeTab === 'department' ? 'Chỉnh sửa phòng ban' : 'Chỉnh sửa chức vụ'
        }
        initialName={
          modalState.mode === 'edit' && modalState.entity
            ? (modalState.entity.tenPhongBan || modalState.entity.tenChucVu || modalState.entity.name)
            : ''
        }
        onSave={handleSaveModal}
        isLoading={isCrudLoading}
      />
    </div>
  );
}

export default function PermissionsPage() {
  return (
    <PermissionsProvider>
      <PermissionsPageContent />
    </PermissionsProvider>
  );
}

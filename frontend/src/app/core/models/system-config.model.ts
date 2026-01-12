export type ConfigDataType = 'String' | 'Number' | 'Boolean' | 'Json';

export interface SystemConfig {
  id: number;
  configKey: string;
  configValue: string | null;
  dataType: ConfigDataType;
  description?: string | null;
  isEditable: boolean;
  updatedBy?: number | null;
  updatedByName?: string | null;
  updatedAt: string;
}

export interface UpsertSystemConfig {
  configKey: string;
  configValue?: string | null;
  dataType?: ConfigDataType;
  description?: string | null;
  isEditable?: boolean;
}

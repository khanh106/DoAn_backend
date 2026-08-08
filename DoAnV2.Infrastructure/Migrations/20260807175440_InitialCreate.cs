using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DoAnV2.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "qr_codes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    target_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    target_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    qr_value = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_qr_codes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    role_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_roles", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    full_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    email = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    password_hash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    role_id = table.Column<int>(type: "int", nullable: false),
                    wallet_address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    encrypted_private_key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_users", x => x.id);
                    table.ForeignKey(
                        name: "fk_users_roles_role_id",
                        column: x => x.role_id,
                        principalTable: "roles",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "farm_areas",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    processor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    owner_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    province = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    district = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ward = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    area = table.Column<double>(type: "float", nullable: false),
                    soil_type = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    gps = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    planting_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_farm_areas", x => x.id);
                    table.ForeignKey(
                        name: "fk_farm_areas_users_processor_id",
                        column: x => x.processor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "fruit_types",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    processor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_fruit_types", x => x.id);
                    table.ForeignKey(
                        name: "fk_fruit_types_users_processor_id",
                        column: x => x.processor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "material_items",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    processor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    item_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    quantity_in_stock = table.Column<double>(type: "float", nullable: false),
                    dosage_per_ha = table.Column<double>(type: "float", nullable: true),
                    concentration = table.Column<double>(type: "float", nullable: true),
                    supplier = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    npk_ratio = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_material_items", x => x.id);
                    table.ForeignKey(
                        name: "fk_material_items_users_processor_id",
                        column: x => x.processor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "production_processes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    processor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_production_processes", x => x.id);
                    table.ForeignKey(
                        name: "fk_production_processes_users_processor_id",
                        column: x => x.processor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "products",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    fruit_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    group_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    product_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    variety = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    short_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_products", x => x.id);
                    table.ForeignKey(
                        name: "fk_products_fruit_types_fruit_type_id",
                        column: x => x.fruit_type_id,
                        principalTable: "fruit_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "inventory_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    material_item_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    transaction_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quantity = table.Column<double>(type: "float", nullable: false),
                    transaction_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inventory_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_inventory_logs_material_items_material_item_id",
                        column: x => x.material_item_id,
                        principalTable: "material_items",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_inventory_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "process_steps",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    process_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    stage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    step_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    order_index = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_process_steps", x => x.id);
                    table.ForeignKey(
                        name: "fk_process_steps_production_processes_process_id",
                        column: x => x.process_id,
                        principalTable: "production_processes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    fruit_type_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    product_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    farm_area_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    planting_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expected_quantity = table.Column<double>(type: "float", nullable: false),
                    representative_worker_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    current_stage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    metadata_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    blockchain_batch_id = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    processor_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    farm_area_id2 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_batches_farm_areas_farm_area_id",
                        column: x => x.farm_area_id,
                        principalTable: "farm_areas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_farm_areas_farm_area_id2",
                        column: x => x.farm_area_id2,
                        principalTable: "farm_areas",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_batches_fruit_types_fruit_type_id",
                        column: x => x.fruit_type_id,
                        principalTable: "fruit_types",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_products_product_id",
                        column: x => x.product_id,
                        principalTable: "products",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_users_processor_id",
                        column: x => x.processor_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "fk_batches_users_representative_worker_id",
                        column: x => x.representative_worker_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "batch_workers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    is_representative = table.Column<bool>(type: "bit", nullable: false),
                    assigned_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_batch_workers", x => x.id);
                    table.ForeignKey(
                        name: "fk_batch_workers_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_batch_workers_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cultivation_logs",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    activity_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    log_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    metadata_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_urls_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_cultivation_logs", x => x.id);
                    table.ForeignKey(
                        name: "fk_cultivation_logs_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_cultivation_logs_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "harvests",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    representative_user_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    harvest_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    quantity = table.Column<double>(type: "float", nullable: false),
                    unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    initial_quality = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    metadata_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_harvests", x => x.id);
                    table.ForeignKey(
                        name: "fk_harvests_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_harvests_users_representative_user_id",
                        column: x => x.representative_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "processings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    process_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    start_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    end_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    metadata_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_urls_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_processings", x => x.id);
                    table.ForeignKey(
                        name: "fk_processings_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "sub_batches",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    sub_batch_code = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    parent_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    classification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    quantity = table.Column<double>(type: "float", nullable: false),
                    package_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    qr_code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    current_stage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    metadata_uri = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    data_hash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_sub_batches", x => x.id);
                    table.ForeignKey(
                        name: "fk_sub_batches_batches_parent_batch_id",
                        column: x => x.parent_batch_id,
                        principalTable: "batches",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "blockchain_transactions",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sub_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    wallet_address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    transaction_hash = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    contract_address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    function_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    block_number = table.Column<long>(type: "bigint", nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    error_message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    sub_batch_id2 = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_blockchain_transactions", x => x.id);
                    table.ForeignKey(
                        name: "fk_blockchain_transactions_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_blockchain_transactions_sub_batches_sub_batch_id",
                        column: x => x.sub_batch_id,
                        principalTable: "sub_batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_blockchain_transactions_sub_batches_sub_batch_id2",
                        column: x => x.sub_batch_id2,
                        principalTable: "sub_batches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "inspections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sub_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    asset_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    document_number = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    inspection_unit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    inspection_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    result = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    file_uri = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_inspections", x => x.id);
                    table.ForeignKey(
                        name: "fk_inspections_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_inspections_sub_batches_sub_batch_id",
                        column: x => x.sub_batch_id,
                        principalTable: "sub_batches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "packagings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sub_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    asset_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pack_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    weight = table.Column<double>(type: "float", nullable: false),
                    specification = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    usage_guide = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    storage_guide = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    color = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    smell = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    standard = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    image_urls_json = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_packagings", x => x.id);
                    table.ForeignKey(
                        name: "fk_packagings_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_packagings_sub_batches_sub_batch_id",
                        column: x => x.sub_batch_id,
                        principalTable: "sub_batches",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    sub_batch_id = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    asset_type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    pickup_location = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    destination = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    retailer_id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    carrier_info = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    shipping_code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    shipping_date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    expected_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    received_date = table.Column<DateTime>(type: "datetime2", nullable: true),
                    weight = table.Column<double>(type: "float", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime2", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime2", nullable: true),
                    is_deleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_shipments", x => x.id);
                    table.ForeignKey(
                        name: "fk_shipments_batches_batch_id",
                        column: x => x.batch_id,
                        principalTable: "batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_shipments_sub_batches_sub_batch_id",
                        column: x => x.sub_batch_id,
                        principalTable: "sub_batches",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "fk_shipments_users_retailer_id",
                        column: x => x.retailer_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "roles",
                columns: new[] { "id", "created_at", "description", "is_deleted", "role_name", "updated_at" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(2073), "Quản trị hệ thống", false, "ADMIN", null },
                    { 2, new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(3389), "Nông dân / Công nhân", false, "FARMER", null },
                    { 3, new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(3395), "Hợp tác xã / Doanh nghiệp", false, "PROCESSOR", null },
                    { 4, new DateTime(2026, 8, 7, 17, 54, 39, 210, DateTimeKind.Utc).AddTicks(3397), "Cửa hàng bán lẻ", false, "RETAILER", null }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "id", "created_at", "email", "encrypted_private_key", "full_name", "is_deleted", "password_hash", "phone", "role_id", "status", "updated_at", "wallet_address" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000001"), new DateTime(2026, 8, 7, 17, 54, 39, 505, DateTimeKind.Utc).AddTicks(8242), "admin@gmail.com", null, "System Administrator", false, "$2a$11$7TV6jX8I6zdEUQF.nJvXxu4Yjin0aRevWzBn2pDZH6zkZRZrQk7TG", "0000000000", 1, "APPROVED", null, null });

            migrationBuilder.CreateIndex(
                name: "ix_batch_workers_batch_id_user_id",
                table: "batch_workers",
                columns: new[] { "batch_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_batch_workers_user_id",
                table: "batch_workers",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_batch_code",
                table: "batches",
                column: "batch_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_batches_farm_area_id",
                table: "batches",
                column: "farm_area_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_farm_area_id2",
                table: "batches",
                column: "farm_area_id2");

            migrationBuilder.CreateIndex(
                name: "ix_batches_fruit_type_id",
                table: "batches",
                column: "fruit_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_processor_id",
                table: "batches",
                column: "processor_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_product_id",
                table: "batches",
                column: "product_id");

            migrationBuilder.CreateIndex(
                name: "ix_batches_representative_worker_id",
                table: "batches",
                column: "representative_worker_id");

            migrationBuilder.CreateIndex(
                name: "ix_blockchain_transactions_batch_id",
                table: "blockchain_transactions",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_blockchain_transactions_sub_batch_id",
                table: "blockchain_transactions",
                column: "sub_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_blockchain_transactions_sub_batch_id2",
                table: "blockchain_transactions",
                column: "sub_batch_id2");

            migrationBuilder.CreateIndex(
                name: "ix_blockchain_transactions_transaction_hash",
                table: "blockchain_transactions",
                column: "transaction_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_cultivation_logs_batch_id",
                table: "cultivation_logs",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_cultivation_logs_user_id",
                table: "cultivation_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_farm_areas_processor_id",
                table: "farm_areas",
                column: "processor_id");

            migrationBuilder.CreateIndex(
                name: "ix_fruit_types_processor_id",
                table: "fruit_types",
                column: "processor_id");

            migrationBuilder.CreateIndex(
                name: "ix_harvests_batch_id",
                table: "harvests",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_harvests_representative_user_id",
                table: "harvests",
                column: "representative_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_batch_id",
                table: "inspections",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_inspections_sub_batch_id",
                table: "inspections",
                column: "sub_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_logs_material_item_id",
                table: "inventory_logs",
                column: "material_item_id");

            migrationBuilder.CreateIndex(
                name: "ix_inventory_logs_user_id",
                table: "inventory_logs",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_material_items_processor_id",
                table: "material_items",
                column: "processor_id");

            migrationBuilder.CreateIndex(
                name: "ix_packagings_batch_id",
                table: "packagings",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_packagings_sub_batch_id",
                table: "packagings",
                column: "sub_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_process_steps_process_id",
                table: "process_steps",
                column: "process_id");

            migrationBuilder.CreateIndex(
                name: "ix_processings_batch_id",
                table: "processings",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_production_processes_processor_id",
                table: "production_processes",
                column: "processor_id");

            migrationBuilder.CreateIndex(
                name: "ix_products_fruit_type_id",
                table: "products",
                column: "fruit_type_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_batch_id",
                table: "shipments",
                column: "batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_retailer_id",
                table: "shipments",
                column: "retailer_id");

            migrationBuilder.CreateIndex(
                name: "ix_shipments_sub_batch_id",
                table: "shipments",
                column: "sub_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_sub_batches_parent_batch_id",
                table: "sub_batches",
                column: "parent_batch_id");

            migrationBuilder.CreateIndex(
                name: "ix_sub_batches_sub_batch_code",
                table: "sub_batches",
                column: "sub_batch_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_users_role_id",
                table: "users",
                column: "role_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "batch_workers");

            migrationBuilder.DropTable(
                name: "blockchain_transactions");

            migrationBuilder.DropTable(
                name: "cultivation_logs");

            migrationBuilder.DropTable(
                name: "harvests");

            migrationBuilder.DropTable(
                name: "inspections");

            migrationBuilder.DropTable(
                name: "inventory_logs");

            migrationBuilder.DropTable(
                name: "packagings");

            migrationBuilder.DropTable(
                name: "process_steps");

            migrationBuilder.DropTable(
                name: "processings");

            migrationBuilder.DropTable(
                name: "qr_codes");

            migrationBuilder.DropTable(
                name: "shipments");

            migrationBuilder.DropTable(
                name: "material_items");

            migrationBuilder.DropTable(
                name: "production_processes");

            migrationBuilder.DropTable(
                name: "sub_batches");

            migrationBuilder.DropTable(
                name: "batches");

            migrationBuilder.DropTable(
                name: "farm_areas");

            migrationBuilder.DropTable(
                name: "products");

            migrationBuilder.DropTable(
                name: "fruit_types");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "roles");
        }
    }
}

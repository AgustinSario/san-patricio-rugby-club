# ✅ Photo Upload Feature - Ready for Testing

## Application Status
- **Build**: ✅ Successful (no errors)
- **Application Running**: ✅ Active on http://localhost:5145
- **Database Migration**: ✅ Applied successfully

## What to Test

### 1. Create New Member with Photo
1. Navigate to: http://localhost:5145/Socios/Create
2. Fill in required fields (ApellidoNombre, DNI)
3. Upload a 4x4 photo using the new photo upload section
4. Verify:
   - Photo preview shows correctly
   - Validation works for file size (>2MB), format (only JPG/PNG), and aspect ratio (must be square)
5. Save the member
6. Check the database to verify FotoPath is saved

### 2. Edit Existing Member and Add/Change Photo
1. Go to: http://localhost:5145/Socios
2. Click "Editar" on any member (e.g., ID 1300)
3. Upload a photo (or change existing one)
4. Verify the photo appears in preview
5. Save changes

### 3. Remove Photo from Member
1. Edit a member that has a photo
2. Check "Eliminar foto actual" checkbox
3. Save changes
4. Verify photo is removed from filesystem and database

### 4. Generate Carnet with Photo
1. Go to member details: http://localhost:5145/Socios/Details/{id}
2. Click "Carnet Digital" tab
3. Click "Generar Carnet Digital" button
4. Verify:
   - Member's photo appears centered in the circular badge (same position as escudo was before)
   - Photo is properly cropped in circle with white background and red border
   - All other carnet elements remain unchanged (name, DNI, barcode, etc.)

### 5. Generate Carnet without Photo (Fallback)
1. Edit a member and remove their photo
2. Generate carnet
3. Verify:
   - Club escudo appears instead (original behavior)
   - Everything else works correctly

### 6. View Member Details with Photo
1. Go to member details page
2. Verify:
   - Member's photo appears in the profile card (circular, top-left)
   - Falls back to initial avatar if no photo

## Test Cases for Validation

### Valid Uploads
- ✅ Square JPG photo < 2MB
- ✅ Square PNG photo < 2MB
- ✅ Photo with 4x4 aspect ratio (e.g., 400x400, 800x800, 600x600)

### Invalid Uploads (Should Show Errors)
- ❌ Photo > 2MB (should show size error)
- ❌ Non-square photo (e.g., 800x600) (should show aspect ratio error)
- ❌ PDF, GIF, BMP files (should show format error)

## Files Changed
1. `SanPatricioRugby.DAL\Models\Socio.cs` - Added FotoPath property
2. `SanPatricioRugby.Web\Controllers\SociosController.cs` - Handle photo upload
3. `SanPatricioRugby.Web\Services\CarnetService.cs` - Use member photo in carnet
4. `SanPatricioRugby.Web\Views\Socios\Create.cshtml` - Photo upload UI
5. `SanPatricioRugby.Web\Views\Socios\Edit.cshtml` - Photo upload UI with remove option
6. `SanPatricioRugby.Web\Views\Socios\Details.cshtml` - Show photo in profile
7. `SanPatricioRugby.Web\SanPatricioRugby.Web.csproj` - Added ImageSharp package

## Database
- Migration: `20260408120014_AddFotoPathToSocio`
- Table: Socios
- New Column: FotoPath (nvarchar(max), nullable)

## Storage
- Photos saved to: `wwwroot/images/fotos/foto_{id}.jpg`
- Resized to max: 800x800 pixels
- Compressed: JPEG 75% quality
- Estimated size per photo: ~50-150 KB

## Notes
- Photos are centered in the same position as the escudo was (240px diameter circle)
- White background with red border matching the design
- System gracefully falls back to escudo if no photo exists
- All existing carnets will continue to work with escudo
- Old photos are automatically deleted when uploading new ones
